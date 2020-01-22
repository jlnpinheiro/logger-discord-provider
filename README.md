# Discord .NET Logger Provider
[![AppVayor - Build status](https://ci.appveyor.com/api/projects/status/hqdwvdowbmifop4f?svg=true)](https://ci.appveyor.com/project/jlnpinheiro/logger-discord-provider) [![NuGet](https://img.shields.io/nuget/dt/logger-discord-provider.svg?style=flat-square)](https://www.nuget.org/packages/logger-discord-provider) [![NuGet](https://img.shields.io/nuget/v/logger-discord-provider.svg?style=flat-square)](https://www.nuget.org/packages/logger-discord-provider)

A .NET logger provider to send log entries to **Discord** (https://discordapp.com/) as message in a channel. 

For more information about .NET Core logging API visit [Logging in .NET Core and ASP.NET Core](https://docs.microsoft.com/en-Us/aspnet/core/fundamentals/logging/?view=aspnetcore-3.1) and [Fundamentals of Logging in .NET Core](https://www.tutorialsteacher.com/core/fundamentals-of-logging-in-dotnet-core)

## Target
[Discord Webhook Client](https://github.com/jlnpinheiro/discord-webhook-client)<br>
.NET Standard 2.0+

For more information about suported versions visit https://docs.microsoft.com/pt-br/dotnet/standard/net-standard

## Installation

### NuGet
```
Install-Package logger-discord-provider
```
### .NET CLI
```
dotnet add package logger-discord-provider
```
## Configuration
This sample code shows how to add Discord Logger Provider on a ASP.NET Core API project (Startup.cs file):

```csharp
using JNogueira.Logger.Discord;

namespace My.Sample.Code
{
    public IConfiguration Configuration { get; }
    
    public class Startup
    {
        ... 
        
        // Add parameters of type ILoggerFactory and IHttpContextAccessor
        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
        {
            loggerFactory
                // Add "AddDiscord" method with desired options.
                .AddDiscord(new DiscordLoggerOptions(Configuration["WebhookUrl"])
                {
                    ApplicationName = "Application Name Test",
                    EnvironmentName = "Name of environment",
                    UserName = "Bot username"
                });
                
             app.UseMvc();
        }
    }
}
```

## How to logging
This sample code shows how to add Discord Logger on a ASP.NET Core API controller:

```csharp
using Microsoft.Extensions.Logging;

namespace My.Sample.Code
{
    public class TodoController : Controller
    {
        private readonly ILogger _logger;

        public TodoController(ITodoRepository todoRepository, ILogger<TodoController> logger)
        {
            _logger = logger;
        }
        
        public IActionResult SayHello()
        {
            ...
            
            // Call "LogInformation" to sendo log messages to Discord channel
            _logger.LogInformation("Hello! This is a sample Discord message sent by ASP.NET Core application!");
            
            ...
        }
    }
}
```

## Message types

**Trace**
```csharp
_logger.LogTrace("OK! It's a trace message.");
```

**Debug**
```csharp
_logger.LogDebug("OK! It's a debug message.");
```

**Information**
```csharp
_logger.LogInformation("OK! It's a info message.");
```

**Warning**
```csharp
_logger.LogWarning("OK! It's a warning message.");
```

**Error**
```csharp
 _logger.LogError("OK! It's a error message.");
```

**Critical**
```csharp
 _logger.LogError("OK! It's a error message.");
```
