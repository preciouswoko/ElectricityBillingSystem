using ElectricityBillingSystem.Application.Abstraction;
using ElectricityBillingSystem.Application.Concrete;
using ElectricityBillingSystem.Infrastructure.Data;
using ElectricityBillingSystem.Infrastructure.ElectricityProviders;
using ElectricityBillingSystem.Infrastructure.IServices;
using ElectricityBillingSystem.Infrastructure.Messaging.EventHandlers;
using ElectricityBillingSystem.Infrastructure.Messaging.MockSns;
using ElectricityBillingSystem.Infrastructure.Messaging.MockSqs;
using ElectricityBillingSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register services
builder.Services.AddScoped<IEventPublisher, MockSnsService>();
builder.Services.AddScoped<ISmsService, MockSmsService>();
builder.Services.AddScoped<IElectricityProvider, ProviderA>();
builder.Services.AddScoped<IElectricityProvider, ProviderB>();
builder.Services.AddScoped<IWalletsService, WalletsService>();
builder.Services.AddScoped<IElectricityService, ElectricityService>();

// Configure Mock SNS/SQS
builder.Services.AddSingleton<IMockSnsPublisher, MockSnsPublisher>();

// Configure queues
builder.Services.AddSingleton<IMockSqsQueue>(sp =>
    new MockSqsQueue(
        "bills-queue",
        sp.GetRequiredService<ILogger<MockSqsQueue>>()
    ));

builder.Services.AddSingleton<IMockSqsQueue>(sp =>
    new MockSqsQueue(
        "payments-queue",
        sp.GetRequiredService<ILogger<MockSqsQueue>>()
    ));

// Configure event handlers
//builder.Services.AddHostedService<BillEventHandler>();
//builder.Services.AddHostedService<PaymentEventHandler>();
builder.Services.AddScoped<BillEventHandler>(); // Change from AddSingleton to AddScoped
builder.Services.AddScoped<PaymentEventHandler>(); // Change from AddSingleton to AddScoped

//// JWT configuration
//var key = Encoding.ASCII.GetBytes(Configuration["Jwt:Key"]);
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
//    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
//}).AddJwtBearer(options =>
//{
//    options.TokenValidationParameters = new TokenValidationParameters
//    {
//        ValidateIssuer = true,
//        ValidateAudience = true,
//        ValidateIssuerSigningKey = true,
//        ValidIssuer = Configuration["Jwt:Issuer"],
//        ValidAudience = Configuration["Jwt:Audience"],
//        IssuerSigningKey = new SymmetricSecurityKey(key)
//    };
//});

builder.Services.AddDistributedMemoryCache(); // Add cache service
builder.Services.AddScoped<IUserService, UserService>(); // Assuming you have a repository pattern

// Load JWT settings
var jwtSettings = builder.Configuration.GetSection("Jwt");

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings["Key"]))
    };
});

// Configure authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Bearer", policy =>
    {
        policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
});

// Configure Swagger
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] followed by your token.",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen();
// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)  // Read settings from appsettings.json
    .WriteTo.Console()  // Optional: log to console
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)  // Log to file, rolling daily
    .CreateLogger();
// Replace the default logger with Serilog
builder.Host.UseSerilog();
builder.Services.AddHttpContextAccessor();
var app = builder.Build();

// Set up subscriptions after service provider is built
var sns = app.Services.GetRequiredService<IMockSnsPublisher>();
var billsQueue = app.Services.GetRequiredService<IMockSqsQueue>();
var paymentsQueue = app.Services.GetRequiredService<IMockSqsQueue>();

sns.Subscribe("bill_created", billsQueue);
sns.Subscribe("payment_completed", paymentsQueue);

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Call the seeding method
        await DatabaseSeeder.SeedAsync(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
