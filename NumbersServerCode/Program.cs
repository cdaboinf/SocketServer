using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NumbersServerCode;
using NumbersServerCode.Models;
using NumbersServerCode.Services;

const int maxDefaultValue = 5;
const int delayDefaultValue = 10;

// set up configurations (build key-value pair configurations)
var builder = new ConfigurationBuilder()
    .SetBasePath(Path.Combine(AppContext.BaseDirectory))
    .AddJsonFile("appsettings.json", optional: false);
var config = builder.Build(); // (build key-value pair configurations from sources)

// setup dependencies
var serviceProvider = new ServiceCollection()
    .AddSingleton<ISocketService, SocketService>()
    .AddSingleton<INumbersServer, NumbersServer>()
    .AddSingleton<ILoggerService, LoggerService>()
    .AddOptions()
    .Configure<SocketServiceOptions>(config.GetSection(nameof(SocketServiceOptions)))
    .Configure<LoggerServiceOptions>(config.GetSection(nameof(LoggerServiceOptions)))
    .BuildServiceProvider(); // creates a services provider from the service collections

// get an instance of numbers server
using var serviceScope = serviceProvider.CreateScope(); // create scope to resolve a service
var provider = serviceScope.ServiceProvider; // resolve dependencies from scope
var server = provider.GetRequiredService<INumbersServer>(); // get service T from provider

// connection settings
var maxSetting = config["ServerInputOptions:MaxConcurrentConnections"];
var maxConnections = int.TryParse(maxSetting, out var connValue) ? connValue : maxDefaultValue;
var delaySetting = config["ServerInputOptions:ReportDelayInMilliseconds"];
var reportDelay = int.TryParse(delaySetting, out var delayValue) ? delayValue : delayDefaultValue;
var tokenSource = new CancellationTokenSource(); // use to signal cancellation

// start listening for client connections
await server.ListenToConnectionsAsync(maxConnections, reportDelay, tokenSource);