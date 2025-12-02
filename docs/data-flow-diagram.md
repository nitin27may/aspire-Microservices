# E-Commerce Data Flow Diagram

## Complete User Journey Data Flow

This diagram illustrates the complete data flow through the system from user interaction to order completion, showing how data moves between microservices and infrastructure components.

```mermaid
sequenceDiagram
    actor User
    participant Web as Blazor Web UI
    participant Catalog as Product Catalog API
    participant Users as User Management API
    participant Orders as Orders API
    participant Inventory as Inventory API
    participant Notifications as Notifications API
    participant Cache as Redis Cache
    participant DB as PostgreSQL
    participant MQ as RabbitMQ
    
    Note over User,MQ: Phase 1: Browse Products
    User->>Web: Browse Products
    Web->>Catalog: GET /api/products
    Catalog->>Cache: Check Cache
    alt Cache Hit
        Cache-->>Catalog: Return Cached Data
    else Cache Miss
        Catalog->>DB: Query catalogdb
        DB-->>Catalog: Product Data
        Catalog->>Cache: Store in Cache
    end
    Catalog-->>Web: Product List
    Web-->>User: Display Products
    
    Note over User,MQ: Phase 2: User Authentication
    User->>Web: Register/Login
    Web->>Users: POST /api/auth/register or /login
    Users->>DB: Query/Insert usersdb
    DB-->>Users: User Data
    Users-->>Web: JWT Token
    Web-->>User: Authenticated Session
    
    Note over User,MQ: Phase 3: Add to Cart
    User->>Web: Add Items to Cart
    Note over Web: Cart stored in<br/>browser localStorage
    
    Note over User,MQ: Phase 4: Checkout Process
    User->>Web: Proceed to Checkout
    Web->>Inventory: POST /api/inventory/check
    Inventory->>Cache: Check Stock Cache
    alt Cache Available
        Cache-->>Inventory: Stock Data
    else No Cache
        Inventory->>DB: Query inventorydb
        DB-->>Inventory: Stock Data
        Inventory->>Cache: Update Cache
    end
    Inventory-->>Web: Stock Availability
    
    alt Stock Available
        Web->>Orders: POST /api/orders (with JWT)
        Orders->>DB: Insert into ordersdb
        DB-->>Orders: Order Created
        Orders->>MQ: Publish OrderCreated Event
        Note over MQ: Event contains:<br/>OrderId, Items,<br/>UserId, Total
        
        par Process Inventory
            MQ->>Inventory: OrderCreated Event
            Inventory->>DB: Update inventorydb<br/>(Reduce Stock)
            Inventory->>Cache: Invalidate Cache
            Inventory->>DB: Create Reservation
            DB-->>Inventory: Reservation Created
        and Send Notification
            MQ->>Notifications: OrderCreated Event
            Notifications->>DB: Query User Email
            Notifications->>DB: Log Email in notificationsdb
            Notifications-->>User: Send Email Confirmation
        end
        
        Orders-->>Web: Order Confirmation
        Web-->>User: Display Order Number
    else Out of Stock
        Inventory-->>Web: Stock Unavailable
        Web-->>User: Display Error Message
    end
    
    Note over User,MQ: Phase 5: View Order History
    User->>Web: View My Orders
    Web->>Orders: GET /api/orders (with JWT)
    Orders->>DB: Query ordersdb
    DB-->>Orders: Order History
    Orders-->>Web: Order List
    Web-->>User: Display Orders
```

## Key Data Flow Patterns

### 1. **Read Operations with Caching**
- Product Catalog and Inventory use Redis for caching
- Cache-aside pattern: Check cache first, then database
- Reduces database load and improves response time

### 2. **Event-Driven Architecture**
- Orders API publishes events to RabbitMQ
- Inventory and Notifications consume events asynchronously
- Ensures loose coupling between services

### 3. **Authentication Flow**
- JWT-based authentication via User Management API
- Token passed in headers for subsequent requests
- Stateless authentication across microservices

### 4. **Database per Service**
- Each microservice has its own PostgreSQL database
- Data isolation and independence
- Databases: catalogdb, usersdb, ordersdb, inventorydb, notificationsdb

### 5. **Transactional Consistency**
- Order creation triggers inventory reservation
- Email notification sent after order confirmation
- Eventual consistency via message queue

## Data Storage Details

| Service | Database | Cached Data | Message Events |
|---------|----------|-------------|----------------|
| Product Catalog | catalogdb | Products, Categories | - |
| User Management | usersdb | - | - |
| Orders | ordersdb | - | OrderCreated (Publish) |
| Inventory | inventorydb | Stock Levels | OrderCreated (Consume) |
| Notifications | notificationsdb | - | OrderCreated (Consume) |
