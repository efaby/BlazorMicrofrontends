using System;

namespace Shared.Contracts.Events;

public record ProductSelectedEvent(
    int ProductId,
    string ProductName,
    decimal Price,
    DateTime Timestamp
);

public record CustomerSelectedEvent(
    int CustomerId,
    string CustomerName,
    string Email,
    DateTime Timestamp
);

public record OrderCreatedEvent(
    int OrderId,
    int CustomerId,
    int ProductId,
    decimal Total,
    DateTime Timestamp
);

public record NavigationEvent(
    string TargetMicroFrontend,
    string? Route,
    Dictionary<string, object>? Parameters
);

public record NotificationEvent(
    string Message,
    NotificationType Type,
    DateTime Timestamp
);

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}