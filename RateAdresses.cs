using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Reactive.Linq;

internal sealed class RateAdresses
{
    private readonly IObservable<IAddressAndValue> _onEvent;
    private ImmutableDictionary<IAddress, IObservable<IAddressAndValue>> _adressObservables = ImmutableDictionary<IAddress, IObservable<IAddressAndValue>>.Empty;
    private readonly object _gate = new object();
    internal RateAdresses(IObservable<IAddressAndValue> onEvent, UpdateRate rate)
    {
        Rate = rate;
        _onEvent = onEvent;
    }

    internal event EventHandler<IEnumerable<IAddress>> AdressesChanged;

    public UpdateRate Rate { get; }

    internal IObservable<IAddressAndValue<T>> Observe<T>(IAddress<T> address)
    {
        return Observable.Defer(() => GetOrCreateObservable(address));
    }

    private IObservable<IAddressAndValue<T>> GetOrCreateObservable<T>(IAddress<T> address)
    {
        IObservable<IAddressAndValue> observable;
        if (!_adressObservables.TryGetValue(address, out observable))
        {
            bool added = false;
            lock (_gate)
            {
                if (!_adressObservables.TryGetValue(address, out observable))
                {
                    added = true;
                    observable = _onEvent.OfType<IAddressAndValue>()
                                         .Where(x => Equals(x.Address, address))
                                         .Publish()
                                         .RefCount()
                                         .Finally(() => Remove(address));
                    _adressObservables = _adressObservables.Add(address, observable);
                }
            }
            if (added)
            {
                OnAdressesChanged();
            }
        }
        return observable.OfType<IAddressAndValue<T>>()
                         .Distinct(x => x.Value);
    }

    private void Remove(IAddress address)
    {
        lock (_gate)
        {
            _adressObservables = _adressObservables.Remove(address);
        }
        OnAdressesChanged();
    }

    private void OnAdressesChanged()
    {
        AdressesChanged?.Invoke(this, _adressObservables.Keys);
    }
}