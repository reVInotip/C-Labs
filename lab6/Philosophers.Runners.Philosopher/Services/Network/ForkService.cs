using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataContracts;
using Interface;
using Microsoft.Extensions.Options;

namespace Services.Network;

public class ForkService(IHttpClientFactory client) : IFork
{
    private readonly HttpClient _client = client.CreateClient("fork-client");

    public int Id { get; set; }

    public async Task Put(IPhilosopher philosopher)
    {
        var forecast = new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Put,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        };

        var content = new StringContent(
            JsonSerializer.Serialize(forecast),
            Encoding.UTF8, "application/json"
        );
        await _client.PostAsync("put-or-unlock-fork", content);
    }

    public async Task<bool> TryTake(IPhilosopher philosopher)
    {
        var forecast = new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Take,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        };

        var content = new StringContent(
            JsonSerializer.Serialize(forecast),
            Encoding.UTF8, "application/json"
        );
        var response = await _client.PostAsync("lock-or-take-fork", content);

        using var stream = await response.Content.ReadAsStreamAsync();
        var data = await JsonSerializer.DeserializeAsync<bool>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return data;
    }

    public async Task<bool> TryLock(IPhilosopher philosopher)
    {
        var forecast = new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Lock,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        };

        var content = new StringContent(
            JsonSerializer.Serialize(forecast),
            Encoding.UTF8, "application/json"
        );
        var response = await _client.PostAsync("lock-or-take-fork", content);

        using var stream = await response.Content.ReadAsStreamAsync();
        var data = await JsonSerializer.DeserializeAsync<bool>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return data;
    }

    public async Task UnlockFork(IPhilosopher philosopher)
    {
        var forecast = new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Unlock,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        };

        var content = new StringContent(
            JsonSerializer.Serialize(forecast),
            Encoding.UTF8, "application/json"
        );
        await _client.PostAsync("put-or-unlock-fork", content);
    }
}
