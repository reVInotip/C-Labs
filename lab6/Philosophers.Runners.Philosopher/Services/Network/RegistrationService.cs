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

    public async Task<PhilosopherWithForksIds?> Registration(string name)
    {
        var formData = new Dictionary<string, string> { { "name", name } };
        var content = new FormUrlEncodedContent(formData);
        var response = await _client.PostAsync("register-me", content);

        using var stream = await response.Content.ReadAsStreamAsync();

        return await JsonSerializer.DeserializeAsync<PhilosopherWithForksIds>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
    }
}
