# GreenLeaf Tea Factory — Backend API

A RESTful Web API for the GreenLeaf Tea Factory Management System, built with ASP.NET Core 8 and MySQL. Provides secure, role-based endpoints for managing products, orders, inventory, users, and customer interactions.

## Tech Stack

| Layer | Technology |
|-------|-----------|
| Framework | ASP.NET Core 8 (Web API) |
| Database | MySQL 8+ |
| ORM | Entity Framework Core 8 (Pomelo provider) |
| Authentication | JWT Bearer Tokens |
| Documentation | Swagger / OpenAPI (development) |
| Language | C# 12 |

## Features

### Authentication & Authorization
- JWT-based authentication with role-based access control
- Three user roles: **Admin**, **Staff**, **Customer**
- Secure password hashing (HMACSHA512 with salt)
- Custom 401/403 JSON error responses
- Token expiry configuration via appsettings

### Product & Inventory Management
- Full CRUD for products with categories, grades, and pricing
- Product image upload support (stored in `wwwroot/uploads/`)
- Real-time inventory tracking with quantity and reorder levels
- Category management with product associations

### Order Processing
- Customer order placement with multi-item cart
- Order status workflow: Pending → Processing → Shipped → Delivered
- Order history with line-item details and totals
- Admin order management and status updates

### Dashboard & Analytics
- Admin dashboard with aggregated statistics
- Revenue tracking and order breakdown by status
- Low-stock alerts and inventory summaries
- Recent orders and activity feeds

### Staff Task Management
- Task assignment and tracking for staff members
- Task status workflow: Pending → In Progress → Completed
- Staff-specific views with restricted permissions

### Public Features (No Auth Required)
- Quote request submission (product inquiries with quantity)
- Contact message submission
- Public product listing

## User Roles

| Role | Access Level |
|------|-------------|
| **Admin** | Full system access — manage products, orders, inventory, users, quotes, messages, analytics |
| **Staff** | View orders, manage assigned tasks, view inventory (read-only for most modules) |
| **Customer** | Browse shop, manage cart, place orders, view order history, submit quotes |

## Project Structure

```
Greenleaf_teafactory_project_backend/
├── GreenLeafTeaAPI.sln              # Solution file
└── GreenLeafTeaAPI/
    ├── Controllers/                 # API endpoint controllers
    │   ├── AuthController.cs        # Login, register, token management
    │   ├── CartController.cs        # Shopping cart operations
    │   ├── CategoriesController.cs  # Product category CRUD
    │   ├── ContactMessagesController.cs  # Contact form submissions
    │   ├── DashboardController.cs   # Admin dashboard analytics
    │   ├── InventoryController.cs   # Stock level management
    │   ├── OrdersController.cs      # Order CRUD and status updates
    │   ├── ProductsController.cs    # Product CRUD with image upload
    │   ├── QuoteRequestsController.cs  # Quote request management
    │   ├── StaffTasksController.cs  # Staff task assignment
    │   ├── UploadsController.cs     # File upload handling
    │   └── UsersController.cs       # User management (admin)
    ├── Data/
    │   └── AppDbContext.cs          # EF Core context, model config, seed data
    ├── DTOs/                        # Data Transfer Objects for each module
    ├── Middleware/
    │   └── GlobalExceptionMiddleware.cs  # Centralized error handling
    ├── Migrations/                  # EF Core database migrations
    ├── Models/                      # Entity models (11 tables)
    ├── Properties/
    │   └── launchSettings.json      # Dev server configuration
    ├── Services/
    │   ├── JwtSettings.cs           # JWT configuration model
    │   ├── PasswordHelper.cs        # Password hashing utilities
    │   └── TokenService.cs          # JWT token generation
    ├── wwwroot/uploads/             # Product image storage
    ├── Program.cs                   # Application entry point and middleware pipeline
    ├── appsettings.json             # Base configuration (safe placeholders)
    ├── appsettings.Development.json # Local dev overrides (gitignored)
    └── GreenLeafTeaAPI.csproj       # Project dependencies
```

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [MySQL 8+](https://dev.mysql.com/downloads/) (running on port 3306)

## Setup Instructions

### 1. Clone the repository

```bash
git clone https://github.com/rehenisurutharumina/Greenleaf_teafactory_project_backend.git
cd Greenleaf_teafactory_project_backend
```

### 2. Configure the database connection

Create `GreenLeafTeaAPI/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=GreenLeafTeaDB;user=root;password=YOUR_PASSWORD;SslMode=None;"
  },
  "Jwt": {
    "Key": "YourSecureKeyAtLeast32CharactersLong!"
  }
}
```

> **Note:** `appsettings.Development.json` is gitignored. The base `appsettings.json` contains only safe placeholders.

### 3. Run the application

```bash
cd GreenLeafTeaAPI
dotnet run
```

The API will:
- Automatically create the database and apply migrations on first run
- Seed default roles (Admin, Staff, Customer)
- Seed a default admin account and sample products
- Start listening on `http://localhost:5001`

### 4. Access Swagger UI

Open `http://localhost:5001/swagger` in your browser to explore and test all API endpoints.

## Default Accounts (Seeded)

| Role | Email | Password |
|------|-------|----------|
| Admin | admin@greenleaf.com | Admin@123 |
| Staff | staff@greenleaf.com | Staff@123 |

> These are development-only credentials seeded via EF Core migrations.

## API Endpoints Overview

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/register` | — | Register new customer |
| POST | `/api/auth/login` | — | Login and receive JWT |
| GET | `/api/products` | — | List all products |
| POST | `/api/quoterequests` | — | Submit quote request |
| POST | `/api/contactmessages` | — | Submit contact message |
| GET | `/api/dashboard/overview` | Admin | Dashboard statistics |
| GET | `/api/orders` | Admin | List all orders |
| POST | `/api/cart` | Customer | Add item to cart |
| POST | `/api/orders` | Customer | Place order from cart |
| GET | `/api/stafftasks` | Staff | View assigned tasks |

> For the complete API reference, see the Swagger documentation at `/swagger`.

## Configuration

| Setting | Location | Description |
|---------|----------|-------------|
| DB Connection | `appsettings.Development.json` | MySQL connection string |
| JWT Secret | `appsettings.Development.json` or `JWT_SECRET_KEY` env var | Token signing key (min 32 chars) |
| JWT Issuer/Audience | `appsettings.json` | Token validation parameters |
| CORS Origins | `appsettings.json` → `Frontend.AllowedOrigins` | Allowed frontend URLs |
| Token Expiry | `appsettings.json` → `Jwt.ExpiryHours` | JWT token lifetime (default: 24h) |

## Future Improvements

- Email notification service for order confirmations
- Payment gateway integration
- Product review and rating system
- Export reports (PDF/Excel)
- API rate limiting
- Unit and integration tests

## Related Repository

- **Frontend:** [Greenleaf_teafactory_project_frontend](https://github.com/rehenisurutharumina/Greenleaf_teafactory_project_frontend)