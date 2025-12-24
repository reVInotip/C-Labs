using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DataContracts;
using Interface;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Services.Network;

public class ForkService(IBus bus) : IFork
{
    private readonly IBus _bus = bus;

    public int Id { get; set; }

    public async Task Put(IPhilosopher philosopher, CancellationToken token)
    {
        await _bus.Publish(new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Put,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        }, token);
    }

    public async Task Take(IPhilosopher philosopher, CancellationToken token)
    {
        await _bus.Publish(new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Take,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        }, token);
    }

    public async Task Lock(IPhilosopher philosopher, CancellationToken token)
    {
        await _bus.Publish(new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Lock,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        }, token);
    }

    public async Task UnlockFork(IPhilosopher philosopher, CancellationToken token)
    {
        await _bus.Publish(new ForkCommandWithIdDto
        {
            ForkCommands = ForkCommandsDto.Unlock,
            PhilosopherId = philosopher.Id,
            ForkId = Id
        }, token);
    }
}
