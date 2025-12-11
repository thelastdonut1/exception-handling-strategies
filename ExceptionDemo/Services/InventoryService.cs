using OrderSystem.ExceptionDemo.Exceptions;
using OrderSystem.ExceptionDemo.Repositories;
using OrderSystem.Models;

namespace OrderSystem.ExceptionDemo.Services;

public class InventoryService
{
    private readonly InventoryRepository _repository;
    private readonly ILogger _logger;

    public InventoryService(InventoryRepository repository, ILogger logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public void ReserveItems(List<OrderItem> items)
    {
        if (items == null || items.Count == 0)
            throw new ArgumentException("Cannot reserve empty item list", nameof(items));

        var reservedItems = new List<int>();

        try
        {
            foreach (var orderItem in items)
            {
                ReserveSingleItem(orderItem);
                reservedItems.Add(orderItem.ItemId);
            }

            _logger.Info($"Successfully reserved {items.Count} items");
        }
        catch (InventoryException)
        {
            _logger.Warning($"Reservation failed. {reservedItems.Count} items may need rollback.");
            throw;
        }
        catch (RepositoryException ex)
        {
            _logger.Error($"Repository failure during reservation", ex); // Log #2 (duplicate)
            throw new InventoryException("Failed to reserve items due to database error", ex);
        }
    }

    private void ReserveSingleItem(OrderItem orderItem)
    {
        InventoryItem currentInventory;
        
        try
        {
            currentInventory = _repository.GetItem(orderItem.ItemId);
        }
        catch (RepositoryException ex) when (ex.Message.Contains("not found"))
        {
            // Fragile: checking exception message string
            throw new InventoryException($"Item {orderItem.ItemId} does not exist in inventory", ex,
                itemId: orderItem.ItemId, requestedQuantity: orderItem.Quantity);
        }

        int available = currentInventory.QuantityAvailable - currentInventory.QuantityReserved;
        
        if (available < orderItem.Quantity)
        {
            // Business case using same exception mechanism as infrastructure failures
            throw new InventoryException(
                $"Insufficient inventory for item {orderItem.ItemId}. Requested: {orderItem.Quantity}, Available: {available}",
                itemId: orderItem.ItemId, requestedQuantity: orderItem.Quantity);
        }

        _repository.UpdateQuantity(
            orderItem.ItemId,
            currentInventory.QuantityAvailable,
            currentInventory.QuantityReserved + orderItem.Quantity);
    }
}