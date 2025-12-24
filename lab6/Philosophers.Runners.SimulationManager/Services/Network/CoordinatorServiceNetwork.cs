using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;
using System.Text.Json;
using Interface;

namespace Services.Network;

public class CoordinatorServiceNetwork(IHttpClientFactory client) : ICoordinatorNetwork
{
    private readonly HttpClient _client = client.CreateClient("philosopher-client");

    public async Task<PhilosopherInfo?> GetInfo(string originUri)
    {
        var response = await _client.GetAsync($"?originUri={originUri}");
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherInfo>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<PhilosopherInfo?> GetStats(string originUri, double simulationTime)
    {
        var response = await _client.GetAsync($"stats?simulationTime={simulationTime}&originUri={originUri}");
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherInfo>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task Stop(string originUri)
    {
        await _client.GetAsync($"stop?originUri={originUri}");
    }

    public async Task Stop()
    {
        await _client.GetAsync("stop-me");
    }
}
