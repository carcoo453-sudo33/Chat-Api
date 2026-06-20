---
name: Chat API architecture
description: Key decisions for the .NET 9 Chat API — stack, patterns, naming, and DI wiring
---

## Stack
.NET 9 ASP.NET Core · SignalR · MongoDB.Driver 3.4 (+ in-memory fallback via ChatDbContext) · StackExchange.Redis (optional) · BCrypt.Net-Next · JwtBearer 9.0 · MediatR 12.4.1 · Swashbuckle 6.5

## Request pipeline
Controller → IMediator → MediatR Handler → IUnitOfWork → IRepository<T> → ChatDbContext

## Enum location
All enums live in `Models/Enums/` (RoomType, MessageType, UserRole, UserStatus, FileType).
Entity files import them with `using apiContact.Models.Enums;`.

**Why:** Old codebase had enums inline in entity files causing cross-file reference issues when enums moved. Always add the using.

## Role field
ChatUser.Role is stored as string (e.g. "Admin", "User") for JWT ClaimTypes.Role compat.
ChatUser.RoleEnum is a computed property that parses Role to UserRole enum.

**Why:** ASP.NET [Authorize(Roles="admin")] does case-insensitive match on the string Role claim.

## Repository pattern
- `IRepository<T>` — generic CRUD (GetById, GetAll, Find, Add, Update, Delete, Count, Exists)
- Domain-specific interfaces extend IRepository<T> (IUserRepository, IRoomRepository, IMessageRepository)
- Each concrete repo checks `_db.IsInMemory` internally — single smart implementation, no separate InMemory/Mongo classes
- `IUnitOfWork` exposes Users, Rooms, Messages repositories

## MediatR CQRS
- Features grouped by domain: `Features/Users/`, `Features/Rooms/`, `Features/Messages/`
- Two files per domain: `*Queries.cs` (IRequest returns data) and `*Commands.cs` (IRequest mutates)
- Handlers that need SignalR inject `IHubContext<ChatHub>` directly (SendMessage, EditMessage, DeleteMessage)
- Registered with: `builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()))`

## AuthController
AuthController was updated to use IUnitOfWork directly (instead of IUserService).
IUserService is still registered in DI and used internally by the service layer.

## CORS
Two named policies in Program.cs:
- `DevelopmentPolicy` — AllowAnyOrigin (dev only)
- `ProductionPolicy` — restricts to *.replit.app / *.replit.dev / *.repl.co with AllowCredentials
Selected by: `app.UseCors(isDev ? "DevelopmentPolicy" : "ProductionPolicy")`

## Slug generation
`Utilities/SlugHelper.Generate(name)` → lowercase, hyphenated, accents stripped.
`SlugHelper.Uniquify(slug, existingSlugs)` → appends -2, -3... if taken.
Slug is generated at create time (CreateRoomCommand and RoomService.CreateAsync both generate slug).

## Tags / Category / Pagination / Search
- ChatRoom: Slug, Category (string), Tags (List<string>), IsArchived, IsPrivate
- Message: Tags (List<string>), Reactions (Dictionary<string, List<string>>)
- `PagedQuery` base class has Page, PageSize, Skip, Clamp(maxPageSize)
- `RoomSearchQuery` extends PagedQuery with Q, Category, Tag, Type, Sort
- `MessageSearchQuery` extends PagedQuery with Q, Tag, SenderId, Type, From, To
- `PagedResult<T>` has Items, Total, Page, PageSize, TotalPages, HasNext, HasPrevious

## Seed data
- UserSeed: alice (Admin/Online), bob (User/Offline), carla (User/Away) — password123
- RoomSeed: General (Channel/community), Engineering (Group/engineering), Alice & Bob (Direct/direct)
- MessageSeed: seeded messages with Tags; slugs auto-generated from room names

## File paths for this project
apiContact/ is the project root for dotnet run.
Workflow command: `cd apiContact && dotnet run`
