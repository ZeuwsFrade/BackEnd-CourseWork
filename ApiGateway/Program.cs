var builder = WebApplication.CreateBuilder(args);

// Добавляем YARP (Reverse Proxy)
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// CORS (разрешаем все для разработки)
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

// Middleware
app.UseCors("AllowAll");
app.MapReverseProxy(); // Включаем YARP

app.MapGet("/", () => "API Gateway работает!");
app.MapGet("/health", () => new
{
    status = "ok",
    gateway = "ApiGateway",
    timestamp = DateTime.Now
});

app.Run();