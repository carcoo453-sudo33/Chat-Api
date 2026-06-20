using System.Reflection;
using System.Text;
using apiContact.Data;
using apiContact.Data.Repositories;
using apiContact.Hubs;
using apiContact.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ── Controllers & API explorer ────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── MediatR (CQRS) ────────────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

// ── Swagger with JWT Bearer support ──────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title       = "Chat API",
        Version     = "v1",
        Description = "Realtime chat API · WebSocket (SignalR) · MongoDB · Redis · Blob Storage",
        Contact     = new OpenApiContact { Name = "Chat API", Email = "api@chat.io" }
    });

    c.EnableAnnotations();

    var scheme = new OpenApiSecurityScheme
    {
        Name         = "Authorization",
        Type         = SecuritySchemeType.Http,
        Scheme       = "bearer",
        BearerFormat = "JWT",
        In           = ParameterLocation.Header,
        Description  = "Enter your JWT access token. Example: `eyJhbGci...`"
    };
    c.AddSecurityDefinition("Bearer", scheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = builder.Configuration["Jwt:Issuer"],
            ValidAudience            = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew                = TimeSpan.Zero
        };

        // Allow SignalR to read token from query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                var path  = ctx.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(token) && path.StartsWithSegments("/hubs"))
                    ctx.Token = token;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── SignalR ───────────────────────────────────────────────────────────────────
builder.Services.AddSignalR();

// ── Repository layer (Unit of Work + Repositories) ───────────────────────────
builder.Services.AddScoped<IUserRepository,    UserRepository>();
builder.Services.AddScoped<IRoomRepository,    RoomRepository>();
builder.Services.AddScoped<IMessageRepository, MessageRepository>();
builder.Services.AddScoped<IUnitOfWork,        UnitOfWork>();

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddSingleton<ChatDbContext>();
builder.Services.AddScoped<IAuthService,    AuthService>();
builder.Services.AddScoped<IUserService,    UserService>();
builder.Services.AddScoped<IRoomService,    RoomService>();
builder.Services.AddScoped<IMessageService, MessageService>();
builder.Services.AddScoped<IFileService,    FileService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
var isDev = builder.Environment.IsDevelopment();

builder.Services.AddCors(options =>
{
    // Development — allow any origin (useful for localhost:3000, live-reload, etc.)
    options.AddPolicy("DevelopmentPolicy", policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());

    // Production — allow any origin but require credentials headers to be explicit
    // To restrict, replace AllowAnyOrigin with WithOrigins("https://your-app.replit.app")
    options.AddPolicy("ProductionPolicy", policy =>
        policy.SetIsOriginAllowedToAllowWildcardSubdomains()
              .WithOrigins(
                  "https://*.replit.app",
                  "https://*.replit.dev",
                  "https://*.repl.co")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

builder.WebHost.UseUrls("http://0.0.0.0:5000");

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Chat API v1");
    c.RoutePrefix    = "swagger";
    c.DocumentTitle  = "Chat API — Swagger UI";
    c.InjectStylesheet("/css/swagger-custom.css");
    c.InjectJavascript("/js/swagger-nav.js");
});

app.UseCors(isDev ? "DevelopmentPolicy" : "ProductionPolicy");
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/health", () => new
{
    status    = "healthy",
    timestamp = DateTime.UtcNow,
    version   = "1.0.0",
    services  = new
    {
        auth       = "JWT Bearer",
        websocket  = "SignalR",
        database   = "MongoDB (in-memory fallback)",
        cache      = "Redis (optional)",
        storage    = "Blob (local)",
        pattern    = "Repository + Unit of Work + MediatR CQRS"
    }
});

app.Run();
