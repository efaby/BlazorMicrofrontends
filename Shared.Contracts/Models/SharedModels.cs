using System;

namespace Shared.Contracts.Models;

public record Product
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public decimal Price { get; init; }
    public string? ImageUrl { get; init; }
    public int Stock { get; init; }
}

public record Customer
{
    public int Id { get; init; }
    public required string Name { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public required string Address { get; init; }
}

public record Order
{
    public int Id { get; init; }
    public int CustomerId { get; init; }
    public int ProductId { get; init; }
    public int Quantity { get; init; }
    public decimal Total { get; init; }
    public DateTime OrderDate { get; init; }
    public OrderStatus Status { get; init; }
}

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}