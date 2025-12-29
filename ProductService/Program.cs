using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Services;

var builder = WebApplication.CreateBuilder(args);

// Добавление сервисов
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// База данных MSSQL
var connectionString = builder.Configuration.GetConnectionString("ProductDatabase");
if (string.IsNullOrEmpty(connectionString))
{
    connectionString = "Server=DESKTOP-48KPSB0\\SQLEXPRESS;Database=ProductDb;Trusted_Connection=True;TrustServerCertificate=True;";
}

builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseSqlServer(connectionString));

// HTTP клиент для вызова Auth Service
builder.Services.AddHttpClient<IAuthValidationService, AuthValidationService>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Services:AuthService"] ?? "http://localhost:5001/");
    client.DefaultRequestHeaders.Add("Accept", "application/json");
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Конвейер middleware
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Создаем базу данных при запуске
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    dbContext.Database.EnsureCreated();
}

app.Run();