# AOP.Api

AOP.Api is a .NET 8 Web API project demonstrating Aspect-Oriented Programming (AOP) concepts, including logging, documentation generation, and service/repository patterns.

## Features
- **Aspect-Oriented Logging**: Uses interceptors (see `Interceptors/LoggingInterceptor.cs`) to automatically log method calls, parameters, and execution details across controllers, services, and repositories. This improves traceability and debugging without manual logging code.
- **Flow Documentation Generator**: The `Documentation/FlowDocumentationGenerator.cs` utility parses application logs and generates Markdown documentation of API flows, showing the sequence of method calls, parameters, durations, and responses for each request.
- **Example Controllers and Services**: Includes sample controllers (`OrderController`, `WeatherForecastController`) and corresponding services/repositories to demonstrate layered architecture and AOP integration.
- **.NET 8, C# 12**: Built using the latest .NET and C# features for performance, reliability, and modern syntax.

## Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the API
dotnet run --project AOP.Api/AOP.Api.csproj
```

The API will be available at `https://localhost:5001` or `http://localhost:5000` by default.

## Documentation
- API documentation is available via Swagger UI when running the project.
- Flow documentation can be generated using the `FlowDocumentationGenerator` utility.

## Project Structure
- `Controllers/` - API endpoints
- `Services/` - Business logic
- `Repositories/` - Data access
- `Interceptors/` - AOP logic
- `Documentation/` - Documentation utilities

## Contributing
Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

## License
This project is licensed under the MIT License.
