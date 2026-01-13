using System;
using System.Collections.Concurrent;

namespace Shared.Contracts.Communication;

public class EventAggregator : IEventAggregator
{
    private readonly ConcurrentDictionary<Type, List<Delegate>> _subscriptions = new();
    private readonly object _lock = new();

    public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (!_subscriptions.ContainsKey(eventType))
            {
                _subscriptions[eventType] = new List<Delegate>();
            }

            _subscriptions[eventType].Add(handler);
        }

        Console.WriteLine($"📡 Subscribed to event: {eventType.Name}");
    }

    public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
    {
        if (handler == null)
            throw new ArgumentNullException(nameof(handler));

        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);

                if (handlers.Count == 0)
                {
                    _subscriptions.TryRemove(eventType, out _);
                }
            }
        }

        Console.WriteLine($"📡 Unsubscribed from event: {eventType.Name}");
    }

    public async Task PublishAsync<TEvent>(TEvent eventData) where TEvent : class
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        var eventType = typeof(TEvent);
        List<Delegate> handlersSnapshot;

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                return;
            }

            // Crear una copia para evitar problemas de concurrencia
            handlersSnapshot = new List<Delegate>(handlers);
        }

        Console.WriteLine($"📢 Publishing event: {eventType.Name} to {handlersSnapshot.Count} subscriber(s)");

        // Ejecutar handlers de forma asíncrona
        var tasks = handlersSnapshot
            .Cast<Action<TEvent>>()
            .Select(handler => Task.Run(() =>
            {
                try
                {
                    handler(eventData);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"❌ Error in event handler for {eventType.Name}: {ex.Message}");
                }
            }));

        await Task.WhenAll(tasks);
    }

    public void Publish<TEvent>(TEvent eventData) where TEvent : class
    {
        if (eventData == null)
            throw new ArgumentNullException(nameof(eventData));

        var eventType = typeof(TEvent);
        List<Delegate> handlersSnapshot;

        lock (_lock)
        {
            if (!_subscriptions.TryGetValue(eventType, out var handlers) || handlers.Count == 0)
            {
                return;
            }

            handlersSnapshot = new List<Delegate>(handlers);
        }

        Console.WriteLine($"📢 Publishing event (sync): {eventType.Name} to {handlersSnapshot.Count} subscriber(s)");

        foreach (var handler in handlersSnapshot.Cast<Action<TEvent>>())
        {
            try
            {
                handler(eventData);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Error in event handler for {eventType.Name}: {ex.Message}");
            }
        }
    }

    public int GetSubscriptionCount<TEvent>() where TEvent : class
    {
        var eventType = typeof(TEvent);

        lock (_lock)
        {
            if (_subscriptions.TryGetValue(eventType, out var handlers))
            {
                return handlers.Count;
            }
        }

        return 0;
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            _subscriptions.Clear();
        }

        Console.WriteLine("🧹 All event subscriptions cleared");
    }
}

