using Core.Models;
using InterfaceContracts.Channel;
using Services.Channels.Items;
using Interface;
using Services.Channels.Events;
using Services;
using DataContracts;
using Services.Network;
using Services.Channels;
using Core;

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
builder.Services.AddHttpClient("philosopher-client", cfg =>
{
    cfg.DefaultRequestHeaders.Add("Accept", "application/json");
});

builder.Services.AddSingleton<IChannel<PhilosopherWithForksIdsChannelItem>,
    SimulationManagerToControllerChannel<PhilosopherWithForksIdsChannelItem>>();
builder.Services.AddSingleton<IChannel<CommandAnswerChannelItem>,
    SimulationManagerToControllerChannel<CommandAnswerChannelItem>>();
builder.Services.AddSingleton<IChannel<ForkCommandWithIdChannelItem>,
    SimulationManagerToControllerChannel<ForkCommandWithIdChannelItem>>();

builder.Services.AddSingleton<ILogger<Waiter>, Logger<Waiter>>();
builder.Services.AddSingleton<ILogger<SimulationManager>, Logger<SimulationManager>>();
builder.Services.AddSingleton<ILogger<DeadlockAnalyzer>, Logger<DeadlockAnalyzer>>();

builder.Services.AddSingleton<CompletionCoordinator>();
builder.Services.AddSingleton<IPhilosopherNetwork, PhilosopherServiceNetwork>();
builder.Services.AddSingleton<PhilosophersStorage>();

builder.Services.AddTransient<IForksFactory, ForksFactory>();
builder.Services.AddSingleton<IPhilosophersFactory, PhilosophersFactory>();

builder.Services.AddHostedService<Waiter>();
builder.Services.AddHostedService<DeadlockAnalyzer>();
builder.Services.AddHostedService<SimulationManager>();

builder.Services.Configure<SimulationManagerConfiguration>(
    builder.Configuration.GetSection(nameof(SimulationManagerConfiguration)));
builder.Services.Configure<ServicesConfigurations>(
    builder.Configuration.GetSection(nameof(ServicesConfigurations)));

var app = builder.Build();

app.UseRouting();
app.MapControllers();

app.Run();
