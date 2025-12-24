using Core.Models;
using InterfaceContracts.Channel;
using Interface.Strategy;
using Services.Strategy;
using Services.Channels;
using Services.Channels.Items;
using Interface;
using Services;
using Services.Network;
using Services.Consumer;
using MassTransit;
using Microsoft.Extensions.Options;
using DataContracts;

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
builder.Services.AddHttpClient("fork-client", cfg =>
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

builder.Services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();
    x.AddConsumer<CommandResultConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        var options = context.GetRequiredService<IOptions<PhilosopherConfiguration>>().Value;
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

        var rawServiceName = options.ServiceName;

        var queueName = rawServiceName
            .ToLowerInvariant()
            .Replace("_", "-");

        cfg.ReceiveEndpoint(queueName, e =>
        {
            e.ConfigureConsumer<CommandResultConsumer>(context);
        });
    });
});

builder.Services.AddSingleton<IChannel<PhilosopherToControllerChannelItem>,
    PhilosopherToControllerChannel<PhilosopherToControllerChannelItem>>();
builder.Services.AddSingleton<IChannel<PhilosopherActionItem>,
    PhilosopherToControllerChannel<PhilosopherActionItem>>();
builder.Services.AddSingleton<IChannel<ApplicationStopItem>,
    PhilosopherToControllerChannel<ApplicationStopItem>>();
builder.Services.AddSingleton<IChannel<CommandExecutionResultItem>,
    PhilosopherToControllerChannel<CommandExecutionResultItem>>();

builder.Services.AddSingleton<IStrategy, LeftRightStrategy>();
builder.Services.AddSingleton<ILogger<PhilosopherService>, Logger<PhilosopherService>>();
builder.Services.AddSingleton<IRegistration, RegistrationService>();

builder.Services.AddSingleton<IFork, ForkService>();
builder.Services.AddSingleton<IFork, ForkService>();

builder.Services.AddHostedService<PhilosopherService>();

builder.Services.Configure<PhilosopherConfiguration>(builder.Configuration.GetSection(nameof(PhilosopherConfiguration)));
builder.Services.Configure<ServicesConfiguration>(builder.Configuration.GetSection(nameof(ServicesConfiguration)));

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
