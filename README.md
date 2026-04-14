# transEstrellaInv - Truck Parts Inventory System

## Description
Inventory management system for truck parts with multi-currency support, batch tracking, and transaction history.

## Technologies
- ASP.NET Core MVC
- Entity Framework Core
- PostgreSQL
- Bootstrap 5

## Features
- Part definition and inventory batch management
- Multi-currency support with Banxico API
- Transaction tracking (inbound/outbound)
- Photo evidence upload
- Purchase order tracking
- Inventory reports

## Setup Instructions
1. Clone repository
2. Update connection string in appsettings.json
3. Run `dotnet restore`
4. Run `dotnet ef database update`
5. Run `dotnet run`

## Environment
- .NET 8.0
- PostgreSQL 15+
