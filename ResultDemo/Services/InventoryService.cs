using OrderSystem.ResultDemo.Common;
using OrderSystem.ResultDemo.Repositories;
using OrderSystem.Models;

namespace OrderSystem.ResultDemo.Services;

/// <summary>
/// No try-catch blocks. No logging. Clean Result composition.
/// </summary>
public class InventoryService
{
    private readonly InventoryRepository _repository;

    public InventoryService(InventoryRepository repository)
    {
        _repository = repository;
    }

    public Result<List<int>> ReserveItems(List<OrderItem> items)
    {
        if (items == null || items.Count == 0)
        {
            return Result<List<int>>.Failure(
                new Error("Cannot reserve empty item list", ErrorCodes.ValidationError));
        }

        var reservedItems = new List<int>();

        foreach (var orderItem in items)
        {
            var result = ReserveSingleItem(orderItem);

            if (result.IsFailure)
            {
                // Explicit partial failure handling - actually do the rollback
                if (reservedItems.Count > 0)
                {
                    foreach (var itemId in reservedItems)
                    {
                        var qty = items.First(i => i.ItemId == itemId).Quantity;
                        ReleaseReservation(itemId, qty); // Best effort rollback
                    }
                }

                return Result<List<int>>.Failure(
                    result.Error!
                        .WithContext("AttemptedItems", items.Count)
                        .WithContext("SuccessfullyReserved", reservedItems.Count)
                        .WithContext("FailedItemId", orderItem.ItemId));
            }

            reservedItems.Add(orderItem.ItemId);
        }

        return Result<List<int>>.Success(reservedItems);
    }

    private Result ReserveSingleItem(OrderItem orderItem)
    {
        var getResult = _repository.GetItem(orderItem.ItemId);

        if (getResult.IsFailure)
        {
            // Check error code - not message string
            if (getResult.Error!.Code == ErrorCodes.EntityNotFound)
            {
                return Result.Failure(
                    new Error($"Item {orderItem.ItemId} does not exist", ErrorCodes.ItemNotFound)
                        .WithContext("ItemId", orderItem.ItemId));
            }

            return Result.Failure(getResult.Error!.Wrap(
                $"Failed to check inventory for item {orderItem.ItemId}", ErrorCodes.ReservationFailed));
        }

        var inventory = getResult.Data!;
        int available = inventory.QuantityAvailable - inventory.QuantityReserved;

        if (available < orderItem.Quantity)
        {
            // Business case - same Result type, different error code
            return Result.Failure(
                new Error($"Insufficient inventory for item {orderItem.ItemId}", ErrorCodes.InsufficientInventory)
                    .WithContext("ItemId", orderItem.ItemId)
                    .WithContext("RequestedQuantity", orderItem.Quantity)
                    .WithContext("AvailableQuantity", available));
        }

        return _repository.UpdateQuantity(
            orderItem.ItemId,
            inventory.QuantityAvailable,
            inventory.QuantityReserved + orderItem.Quantity);
    }

    public Result ReleaseReservation(int itemId, int quantity)
    {
        var getResult = _repository.GetItem(itemId);
        if (getResult.IsFailure)
            return Result.Failure(getResult.Error!);

        var current = getResult.Data!;
        return _repository.UpdateQuantity(itemId, current.QuantityAvailable,
            Math.Max(0, current.QuantityReserved - quantity));
    }
}