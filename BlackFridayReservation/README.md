# Architecture Overview & Key Trade-offs

## 1. How the System Works (System Workflow)

Our **Black Friday Sale Reservation Engine** is built as a **Multi-Tenant White-Label Service**. This means it can safely handle high-traffic flash sales for multiple clients at the same time.

Here is the step-by-step path every request takes:

1. **Tenant Check**: When a request comes in, the system reads the `X-Tenant-Key` header to figure out which client store it belongs to.
2. **Idempotency & Duplicate Check**: Before touching stock, we check if this request key was already processed. If it’s a duplicate with the same data, we simply return the previous result instead of deducting stock twice.
3. **Atomic Stock Decrement**: The system runs a direct, safe SQL update (`UPDATE ... WHERE RemainingQuantity >= @Quantity`) inside a single database transaction.
4. **Reservation & Job Creation**: If stock is available, we write two things in the same transaction:
* The customer's reservation record.
* A durable background job entry.


5. **Fast Response**: Once the transaction commits, we instantly return a `201 Created` response to the user.
6. **Background Worker Processing**: In the background, our dedicated worker picks up pending jobs to perform downstream tasks (creating the actual order, syncing inventory, and sending notifications). If anything fails, it retries automatically with a delay or moves bad jobs to a Dead-Letter Queue.

---

## 2. Key Architecture Trade-offs

Building high-performance systems is all about making intentional trade-offs. Here is why we made specific design choices:

### 1. Atomic SQL Updates vs. Heavy Database Locks

* **Our Choice**: We update stock using conditional SQL queries directly in the database (`RemainingQuantity >= Quantity`).
* **Why**: It completely prevents **overselling** under high concurrency without locking entire tables or blocking other users.
* **The Trade-off**: Under extreme traffic spikes on a single product, database row contention increases. However, protecting inventory accuracy and financial correctness is our top priority.

### 2. SQL-Backed Job Queue vs. External Message Brokers (RabbitMQ/Kafka)

* **Our Choice**: We store background jobs in SQL Server alongside reservations instead of using external tools like RabbitMQ.
* **Why**: This gives us **atomic ACID transactions**. Reducing stock, creating the reservation, and queuing the background task all succeed together or fail together.
* **The Trade-off**: Checking the database for pending jobs adds a small amount of extra query load to SQL Server compared to a dedicated broker. However, it keeps the system simple and reliable without complex multi-system setups.

### 3. In-Memory Cache vs. Distributed Redis Cache

* **Our Choice**: We use `.NET IMemoryCache` for fast catalog and event metadata reads.
* **Why**: It responds in microseconds and requires no external network calls.
* **The Trade-off**: Cache isn't automatically shared between multiple web app instances. To stay safe, **we never cache active stock counts**—stock checks always hit SQL Server directly to remain 100% accurate.

### 4. Shared Database with Composite Keys vs. Database-per-Tenant

* **Our Choice**: All tenants share one database, but every table relies on composite keys (`TenantId`, `Id`).
* **Why**: It makes infrastructure management, migrations, and deployment much simpler and cheaper.
* **The Trade-off**: A coding mistake could theoretically leak data between tenants. We prevent this by enforcing strict composite foreign key constraints directly inside SQL Server.

### 5. At-Least-Once Delivery

* **Our Choice**: Our background worker guarantees that every job is processed **at least once**.
* **Why**: It ensures no customer orders or notifications are lost during server restarts or network glitches.
* **The Trade-off**: Downstream services must be idempotent (safe to receive the same task more than once) so customers don't get double notifications or duplicate order entries.

---

## 3. How to Test the API (Interactive Swagger UI)

To manually verify the API functionality, inspect endpoints, and test multi-tenant isolation, you can use the built-in interactive **Swagger UI**:

1. **Launch the Application**: Run the API project locally using:
```powershell
dotnet run --project src/BlackFriday.Api

```


2. **Open Swagger UI**: Navigate to your browser and open:
`http://localhost:5000/swagger`
3. **Authenticate with Tenant Key**:
* Click the **Authorize** button located at the top right of the Swagger interface.
* Enter your tenant identifier (e.g., `tenant-electronics` or `tenant-fashion`) into the `X-Tenant-Key` security dialog and click **Authorize**. This automatically injects the required tenant header into all subsequent requests.


4. **Execute Requests**:
* Expand endpoints like `GET /api/sale-events` or `POST /api/reservations`, click **Try it out**, fill in the payload if required, and click **Execute** to inspect live responses, error handling, and business logic execution.