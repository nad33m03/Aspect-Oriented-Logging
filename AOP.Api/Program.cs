using Serilog;
using Castle.DynamicProxy;
using AOP.Api.Services;
using AOP.Api.Repositories;
using AOP.Api.Interceptors;
using AOP.Api.Models;
using AOP.Api.Documentation;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// Add Serilog configuration
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(Path.Combine("logs", "app.log"), shared: true)
    .CreateLogger();
builder.Host.UseSerilog();

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Castle DynamicProxy and LoggingInterceptor
builder.Services.AddSingleton<ProxyGenerator>();
builder.Services.AddTransient<LoggingInterceptor>();


// Helper for proxy registration
static TInterface CreateProxy<TInterface, TImplementation>(IServiceProvider provider)
    where TInterface : class
    where TImplementation : class, TInterface, new()
{
    var proxyGen = provider.GetRequiredService<ProxyGenerator>();
    var logger = provider.GetRequiredService<ILogger<LoggingInterceptor>>();
    var interceptor = new LoggingInterceptor(logger);
    var instance = new TImplementation();
    return proxyGen.CreateInterfaceProxyWithTarget<TInterface>(instance, interceptor);
}

// Helper for service proxy registration with dependency
static TInterface CreateServiceProxy<TInterface, TImplementation, TDependency>(IServiceProvider provider)
    where TInterface : class
    where TImplementation : class, TInterface
    where TDependency : class
{
    var proxyGen = provider.GetRequiredService<ProxyGenerator>();
    var logger = provider.GetRequiredService<ILogger<LoggingInterceptor>>();
    var interceptor = new LoggingInterceptor(logger);
    var dependency = provider.GetRequiredService<TDependency>();
    var instance = (TImplementation)Activator.CreateInstance(typeof(TImplementation), dependency)!;
    return proxyGen.CreateInterfaceProxyWithTarget<TInterface>(instance, interceptor);
}

// Register repositories with LoggingInterceptor
builder.Services.AddScoped<IWeatherForecastRepository>(provider =>
    CreateProxy<IWeatherForecastRepository, WeatherForecastRepository>(provider));
builder.Services.AddScoped<IOrderRepository>(provider =>
    CreateProxy<IOrderRepository, OrderRepository>(provider));

// Register services with LoggingInterceptor
builder.Services.AddScoped<IWeatherForecastService>(provider =>
    CreateServiceProxy<IWeatherForecastService, WeatherForecastService, IWeatherForecastRepository>(provider));
builder.Services.AddScoped<IOrderService>(provider =>
    CreateServiceProxy<IOrderService, OrderService, IOrderRepository>(provider));

var app = builder.Build();

// Create logs directory if it doesn't exist
var logsPath = Path.Combine(builder.Environment.ContentRootPath, "logs");
Directory.CreateDirectory(logsPath);

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Serve static files from logs directory
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(logsPath),
    RequestPath = "/logs"
});

app.UseAuthorization();

app.MapControllers();

app.Run();
