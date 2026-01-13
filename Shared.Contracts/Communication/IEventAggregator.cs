using System;

namespace Shared.Contracts.Communication;

public interface IEventAggregator
{
    void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;
    Task PublishAsync<TEvent>(TEvent eventData) where TEvent : class;
    void Publish<TEvent>(TEvent eventData) where TEvent : class;
}
