using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using InterfaceContracts.Channel;
using Services.Channels.Items;

namespace Services.Channels;

public class PhilosopherToControllerChannel<T>: IChannel<T>
    where T: class, IChannelItem
{
    private readonly Channel<T> _channel;

    public ChannelWriter<T> Writer => _channel.Writer;
    public ChannelReader<T> Reader => _channel.Reader;

    public event EventHandler? SendMeItem;
    public event EventHandler<IChannelEventArgs>? SendMeItemBy;
    public event EventHandler? PublisherWantToRegister;

    public PhilosopherToControllerChannel()
    {
        _channel = Channel.CreateBounded<T>(
            new BoundedChannelOptions(500)
            {
                FullMode = BoundedChannelFullMode.Wait
            }
        );
    }

    public void Notify(object? sender) => SendMeItem?.Invoke(sender, EventArgs.Empty);

    public void NotifyWith(object? sender, IChannelEventArgs args) => SendMeItemBy?.Invoke(sender, args);
}
