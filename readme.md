# E-Commerce Microservices Demo with .NET Aspire

A production-quality, end-to-end e-commerce demonstration showcasing microservices architecture orchestrated by .NET Aspire. This demo serves as both a learning resource and a reference implementation demonstrating real-world microservice patterns, service communication, and distributed system orchestration.

## Architecture Overview

### Microservices
- **Product Catalog API** - Product browsing, search, category management with Redis caching
- **User Management API** - Registration, authentication (JWT), profile management with ASP.NET Core Identity
- **Order Processing API** - Order creation, order history, order status with RabbitMQ event publishing
- **Inventory API** - Stock management, reservation, availability checks with Redis caching
- **Notification API** - Email confirmations, order status notifications via RabbitMQ consumers
- **Blazor Web UI** - Customer-facing responsive web interface with Bootstrap 5.3

### Infrastructure Components
- **PostgreSQL** - Primary data store (separate databases per service)
- **Redis** - Distributed caching for product catalog and inventory
- **RabbitMQ** - Asynchronous event-driven communication between services
- **.NET Aspire AppHost** - Orchestration and service discovery

### Architecture Diagrams
For detailed architecture and data flow visualizations, see:
- [Architecture Overview Diagram](docs/architecture-overview.md) - Complete system architecture with all microservices and infrastructure
- [Data Flow Diagram](docs/data-flow-diagram.md) - End-to-end user journey showing data flow through the system

## Getting Started

### Prerequisites
- .NET 9 SDK
- Docker Desktop (for running PostgreSQL, Redis, and RabbitMQ)

### Running the Application

1. Clone the repository:
```bash
git clone https://github.com/nitin27may/aspire-Microservices.git
cd aspire-Microservices
```

2. Run the AppHost:
```bash
cd src/ECommerce.AppHost
dotnet run
```

3. Open the Aspire Dashboard URL shown in the console (typically https://localhost:17198)

4. Click on the Web Frontend endpoint to access the e-commerce UI

### Demo Credentials
- **Email:** demo@example.com
- **Password:** Demo123!

## User Journey

1. **Browse Products** - View products by category, search, sort, and filter
2. **Add to Cart** - Products are stored in browser localStorage
3. **Checkout** - Requires authentication, validates inventory
4. **Order Confirmation** - Receives order number, email notification sent

## Database Schema

### Databases
- `catalogdb` - Products and Categories
- `usersdb` - User accounts (ASP.NET Core Identity)
- `ordersdb` - Orders and OrderItems
- `inventorydb` - Inventory and Reservations
- `notificationsdb` - Email logs

## Technology Stack

- **.NET 9** - Application framework
- **.NET Aspire** - Cloud-native orchestration
- **Blazor Server** - Interactive web UI
- **Entity Framework Core** - Data access
- **ASP.NET Core Identity** - Authentication
- **JWT Bearer** - Token-based auth
- **RabbitMQ** - Message broker
- **Redis** - Caching
- **PostgreSQL** - Database
- **Scalar** - API documentation (replacement for Swagger)

## Project Structure

```
src/
├── ECommerce.AppHost/           # Aspire orchestration host
├── ECommerce.ServiceDefaults/   # Shared service configuration
├── ECommerce.ProductCatalog.Api/ # Product catalog microservice
├── ECommerce.UserManagement.Api/ # User management microservice
├── ECommerce.Orders.Api/        # Order processing microservice
├── ECommerce.Inventory.Api/     # Inventory microservice
├── ECommerce.Notifications.Api/ # Notification microservice
├── ECommerce.Web/               # Blazor web frontend
└── Shared/
    └── ECommerce.Shared.Contracts/ # Shared event contracts
```

## API Endpoints

Each API service exposes Scalar API documentation at `/scalar/v1`.

### Product Catalog API
- `GET /api/categories` - List all categories
- `GET /api/products` - List products (with filtering, sorting, pagination)
- `GET /api/products/{id}` - Get product details
- `GET /api/products/featured` - Get featured products

### User Management API
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login and get JWT token
- `GET /api/users/{userId}` - Get user profile

### Orders API
- `POST /api/orders` - Create new order (requires auth)
- `GET /api/orders` - Get user's orders (requires auth)
- `GET /api/orders/{orderNumber}` - Get order details (requires auth)

### Inventory API
- `GET /api/inventory/{productId}` - Get inventory for product
- `POST /api/inventory/check` - Check stock availability
- `POST /api/inventory/reserve` - Reserve inventory

## Email Configuration

By default, the notification service logs emails to the database. For actual email delivery:

1. Configure SMTP in `appsettings.json`:
```json
{
  "Smtp": {
    "Host": "smtp.example.com",
    "Port": 587,
    "User": "your-email@example.com",
    "Password": "your-password",
    "SenderEmail": "noreply@ecommerce.demo",
    "SenderName": "E-Commerce Demo"
  }
}
```

2. Or use SMTP4Dev for local development

## Observability

The Aspire Dashboard provides:
- **Distributed Tracing** - Track requests across services
- **Logs** - Centralized logging
- **Metrics** - Service health monitoring
- **Service Discovery** - View all running services

## Seed Data

The application comes pre-seeded with:
- 5 Categories (Electronics, Clothing, Home & Garden, Sports & Outdoors, Books)
- 20 Products (4 per category)
- 2 Test Users (demo@example.com, test@example.com)
- Initial inventory (100 units per product)

## License

This project is for demonstration purposes.
