using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Interface;
using DataContracts;
using System.Text;
using System.Text.Json;


namespace Services.Network;

public class RetranslationService(IHttpClientFactory clientFactory) : IRetranslationService
{
    private readonly HttpClient _commandsClient = clientFactory.CreateClient("commands-client");
    private readonly HttpClient _registrationClient = clientFactory.CreateClient("registration-client");
    private readonly HttpClient _philosopherClient = clientFactory.CreateClient("philosopher-client");

    public async Task<bool> RetranslateCommand(ForkCommandsDto command, int philosopherId, int forkId)
    {
        var forecast = new ForkCommandWithIdDto
        {
            ForkCommands = command,
            PhilosopherId = philosopherId,
            ForkId = forkId
        };

        var content = new StringContent(
            JsonSerializer.Serialize(forecast),
            Encoding.UTF8, "application/json"
        );
        var response = await _commandsClient.PostAsync("execute-command", content);

        using var stream = await response.Content.ReadAsStreamAsync();
        var ok = await JsonSerializer.DeserializeAsync<bool>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        
        return ok;
    }

    public async Task<PhilosopherWithForksIds?> RetranslateRegistration(string name, string uri)
    {
        var forecast = new RegistrationDto(name, uri);

        var content = new StringContent(
            JsonSerializer.Serialize(forecast),
            Encoding.UTF8, "application/json"
        );

        var response = await _registrationClient.PostAsync("register-me", content);

        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherWithForksIds>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<PhilosopherInfo?> RetranslateGetPhilosopherInfo(string uri)
    {
        var response = await _philosopherClient.GetAsync(uri);
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherInfo>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<PhilosopherInfo?> RetranslateGetPhilosopherStats(double simulationTime, string originUri)
    {
        var response = await _philosopherClient.GetAsync(originUri + $"stats?simulationTime={simulationTime}");
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherInfo>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task RetranslateStopApplication(string originUri)
    {
        await _philosopherClient.GetAsync(originUri + "stop");
    }
}
