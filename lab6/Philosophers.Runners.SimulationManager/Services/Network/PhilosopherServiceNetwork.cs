using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataContracts;
using System.Text.Json;
using Interface;

namespace Services.Network;

public class PhilosopherServiceNetwork(IHttpClientFactory client) : IPhilosopherNetwork
{
    private readonly HttpClient _client = client.CreateClient("philosopher-client");

    public async Task<PhilosopherInfo?> GetInfo(string uri)
    {
        var response = await _client.GetAsync(uri);
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherInfo>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<PhilosopherInfo?> GetStats(string uri, double simulationTime)
    {
        var response = await _client.GetAsync(uri + $"stats?simulationTime={simulationTime}");
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherInfo>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task<PhilosopherAction?> GetAction(string uri)
    {
        var response = await _client.GetAsync(uri + "action");
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherAction>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }

    public async Task Stop(string uri)
    {
        await _client.GetAsync(uri + "stop");
    }
}
