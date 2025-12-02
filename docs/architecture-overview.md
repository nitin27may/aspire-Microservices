# E-Commerce Microservices Architecture

## Architecture Overview

This diagram shows the complete microservices architecture of the e-commerce application with all components and infrastructure.

```mermaid
flowchart TB
    subgraph Frontend["Presentation Layer"]
        Web["Blazor Web UI<br/>(Customer Interface)"]
    end
    
    subgraph Orchestration["Orchestration Layer"]
        Aspire[".NET Aspire AppHost<br/>(Service Discovery & Orchestration)"]
    end
    
    subgraph Microservices["Microservices Layer"]
        Catalog["Product Catalog API<br/>(Products, Categories, Search)"]
        Users["User Management API<br/>(Auth, JWT, Identity)"]
        Orders["Orders API<br/>(Order Processing)"]
        Inventory["Inventory API<br/>(Stock Management)"]
        Notifications["Notifications API<br/>(Email Service)"]
    end
    
    subgraph Infrastructure["Infrastructure Layer"]
        Postgres[("PostgreSQL<br/>(5 Databases)")]
        Redis[("Redis<br/>(Distributed Cache)")]
        RabbitMQ["RabbitMQ<br/>(Message Broker)"]
    end
    
    %% Frontend to Microservices
    Web -->|HTTP/REST| Catalog
    Web -->|HTTP/REST| Users
    Web -->|HTTP/REST| Orders
    Web -->|HTTP/REST| Inventory
    
    %% Orchestration
    Aspire -.->|Manages| Catalog
    Aspire -.->|Manages| Users
    Aspire -.->|Manages| Orders
    Aspire -.->|Manages| Inventory
    Aspire -.->|Manages| Notifications
    
    %% Microservices to Infrastructure
    Catalog -->|EF Core| Postgres
    Users -->|EF Core| Postgres
    Orders -->|EF Core| Postgres
    Inventory -->|EF Core| Postgres
    Notifications -->|EF Core| Postgres
    
    %% Caching
    Catalog <-->|Cache| Redis
    Inventory <-->|Cache| Redis
    
    %% Message Queue
    Orders -->|Publish Events| RabbitMQ
    RabbitMQ -->|Consume Events| Inventory
    RabbitMQ -->|Consume Events| Notifications
    
    style Frontend fill:#4A90E2,stroke:#2E5C8A,stroke-width:3px,color:#fff
    style Orchestration fill:#F5A623,stroke:#D68910,stroke-width:3px,color:#fff
    style Microservices fill:#7B68EE,stroke:#5B48CE,stroke-width:3px,color:#fff
    style Infrastructure fill:#50C878,stroke:#3BA860,stroke-width:3px,color:#fff
```

### Component Description

#### Presentation Layer
- **Blazor Web UI**: Customer-facing responsive web interface built with Blazor Server and Bootstrap 5.3

#### Microservices Layer
- **Product Catalog API**: Manages products, categories, search, and filtering with Redis caching
- **User Management API**: Handles user registration, authentication (JWT), and profile management using ASP.NET Core Identity
- **Orders API**: Processes orders, maintains order history, and publishes events to RabbitMQ
- **Inventory API**: Manages stock levels, reservations, and availability checks with Redis caching
- **Notifications API**: Sends email confirmations and order notifications by consuming RabbitMQ events

#### Infrastructure Layer
- **.NET Aspire AppHost**: Orchestrates all services and provides service discovery
- **PostgreSQL**: Primary data store with separate databases per service (catalogdb, usersdb, ordersdb, inventorydb, notificationsdb)
- **Redis**: Distributed caching for product catalog and inventory data
- **RabbitMQ**: Message broker for asynchronous event-driven communication between services
