# WebDevStd2531

A microservices-based e-commerce system built with **ASP.NET Core** and **gRPC**, developed as coursework for a Web Development / Microservices subject. Started as a monolithic MVC app and was refactored into independently deployable services communicating over gRPC and RabbitMQ.

## Architecture

| Service | Responsibility | Port (Docker) |
|---|---|---|
| `WebDevStd2531` (Web) | Public-facing MVC storefront + Admin area, ASP.NET Identity | 8080 |
| `UserService` | User registration, login, JWT issuance | 8081 |
| `CatalogService` | Products, categories, stock | 8082 |
| `OrderService` | Cart, checkout, payments | 8083 |

Supporting infrastructure: **SQL Server** (one database per service context) and **RabbitMQ** (async event exchange).

```
Client → Web (MVC) ──gRPC──> UserService
                     ──gRPC──> CatalogService
                     ──gRPC──> OrderService ──gRPC──> CatalogService (stock checks)
                     ──AMQP──> RabbitMQ (domain events)
```

## Key Technical Features

- **Independently deployable services** — each service has its own Dockerfile and database connection string.
- **gRPC contracts** — Protobuf-defined services with both:
  - **Unary RPCs**: login, product detail lookup, cart mutations (add/remove/increment/decrement), payment
  - **Server-streaming RPCs**: `StreamCartItems`, `StreamProducts`, `StreamUsers`
- **Inter-service security** — JWT issuer/audience validation scoped per service.
- **Asynchronous messaging** — a background RabbitMQ consumer (`RabbitMqEventConsumerHostedService`) with automatic reconnect/retry, listening on a fanout exchange for domain events.
- **Identity & roles** — ASP.NET Identity with `Admin`/`User` role seeding and an Admin area for catalog and order management.

## Tech Stack

- C# / .NET 9
- ASP.NET Core MVC
- Entity Framework Core + SQL Server
- gRPC + Protocol Buffers
- RabbitMQ
- Docker & Docker Compose

## Getting Started

### Prerequisites

- Docker & Docker Compose
- (For local dev without Docker) .NET 9 SDK and a local SQL Server instance

### Run everything with Docker Compose

```bash
git clone https://github.com/PTD-coderunner2049/WebDevStd2531.git
cd WebDevStd2531
docker compose up --build
```

This spins up RabbitMQ, SQL Server, and all four services on the Docker network. Once healthy:

- Web app: `http://localhost:8080`
- User Service: `http://localhost:8081`
- Catalog Service: `http://localhost:8082`
- Order Service: `http://localhost:8083`
- RabbitMQ management UI: `http://localhost:15672` (guest/guest)

### Health checks

Each service/database has a Docker healthcheck. The web app also exposes:

- `GET /health` — checks DB connectivity
- `GET /db-health` — simple DB availability check

### Running a single service locally (without Docker)

```bash
cd WebDevStd2531   # the web project folder
dotnet restore
dotnet run
```

Update `appsettings.Development.json` with your local SQL Server connection string and the addresses of the other services (or run them via Docker Compose alongside).

## Project Structure

```
WebDevStd2531/       # Web front end (MVC, Identity, Admin area)
UserService/         # User accounts & auth microservice
CatalogService/      # Product/category microservice
OrderService/        # Cart/order microservice
docker-compose.yml   # Orchestrates the full system
```

## Notes

This project evolved across a semester (weekly increments visible in the migration history) as part of a Web Development / Software Architecture course, moving from a single monolith to a polyrepo/monorepo microservices architecture.
