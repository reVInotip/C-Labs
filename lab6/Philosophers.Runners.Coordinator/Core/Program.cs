using Core.Controllers;
using Core.Models;
using DataContracts;
using Interface;
using InterfaceContracts.Channel;
using MassTransit;
using Services;
using Services.Channels;
using Services.Channels.Items;
using Services.Consumers;
using Services.Network;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
    .AddEnvironmentVariables()
    .AddCommandLine(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

// Add Services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpClient("commands-client", cfg =>
{
    string uri = builder.Configuration["HOST_URI"]!;
    cfg.BaseAddress = new Uri(uri);
    cfg.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("registration-client", cfg =>
{
    string uri = builder.Configuration["HOST_URI"]!;
    cfg.BaseAddress = new Uri(uri);
    cfg.DefaultRequestHeaders.Add("Accept", "application/json");
});
builder.Services.AddHttpClient("philosopher-client", cfg =>
{
    cfg.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CommandsConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var brokerHost = builder.Configuration["BROKER_HOST"]!;
        var brokerPassword = builder.Configuration["BROKER_PASSWORD"]!;
        var brokerUser = builder.Configuration["BROKER_USER"]!;

        cfg.Host(brokerHost, "/", h =>
        {
            h.Username(brokerUser);
            h.Password(brokerPassword);
        });

        cfg.UseMessageRetry(r => r.Exponential(5,
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(10),
            TimeSpan.FromSeconds(2)
        ));

        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddSingleton<IDeadlockAnalyzer, DeadlockAnalyzer>();
builder.Services.AddSingleton<PhilosophersStates>();
builder.Services.AddSingleton<IRetranslationService, RetranslationService>();

builder.Services.AddSingleton<IChannel<CommandChannelItem>, ControllerChannel<CommandChannelItem>>();
builder.Services.AddSingleton<IChannel<ApplicationStopItem>, ControllerChannel<ApplicationStopItem>>();

builder.Services.AddSingleton<ILogger<CoordinatorController>, Logger<CoordinatorController>>();
builder.Services.AddSingleton<ILogger<DeadlockAnalyzer>, Logger<DeadlockAnalyzer>>();
builder.Services.AddSingleton<ILogger<CoordinatorService>, Logger<CoordinatorService>>();
builder.Services.AddSingleton<ILogger<PhilosophersStates>, Logger<PhilosophersStates>>();

builder.Services.AddHostedService<CoordinatorService>();

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
