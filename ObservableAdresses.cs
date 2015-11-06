using System;
using System.Reactive.Disposables;
using System.Reactive.Linq;

internal sealed class ObservableAdresses : IDisposable
{
    private readonly IDisposable _connection;
    private bool _disposed;

    public ObservableAdresses(IPoller poller)
    {
        var onEvent = Observable.Create<IAddressAndValue>(o => {
            EventHandler<IAddressAndValue> handler = (_, e) => o.OnNext(e);
            poller.Read += handler;
            return Disposable.Create(() => poller.Read -= handler);
        }).Publish();
        _connection = onEvent.Connect();
        
        FastAdresses = new RateAdresses(onEvent, UpdateRate.High);
        SlowAdresses = new RateAdresses(onEvent, UpdateRate.Low);
    }

    public RateAdresses FastAdresses { get; }

    public RateAdresses SlowAdresses { get; }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }
        _disposed = true;
        _connection.Dispose();
    }

    private void VerifyDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().FullName);
        }
    }
}