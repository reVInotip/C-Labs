using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataContracts;
using Interface;

namespace Services.Network;

public class RegistrationService(IHttpClientFactory client) : IRegistration
{
    private readonly HttpClient _client = client.CreateClient("registration-client");

    public async Task<PhilosopherWithForksIds?> Registration()
    {
        var response = await _client.GetAsync("register-me");
        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherWithForksIds>(stream);
    }
}
