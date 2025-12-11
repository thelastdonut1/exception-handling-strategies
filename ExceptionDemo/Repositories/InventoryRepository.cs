using OrderSystem.ExceptionDemo.Database;
using OrderSystem.ExceptionDemo.Exceptions;
using OrderSystem.Models;

namespace OrderSystem.ExceptionDemo.Repositories;

public class InventoryRepository
{
    private readonly DbConnectionFactory _connectionFactory;
    private readonly ILogger _logger;

    public InventoryRepository(DbConnectionFactory connectionFactory, ILogger logger)
    {
        _connectionFactory = connectionFactory;
        _logger = logger;
    }

    public InventoryItem GetItem(int itemId)
    {
        var connection = _connectionFactory.CreateConnection();

        try
        {
            connection.Open();
            var results = connection.Query("SELECT * FROM Inventory WHERE ItemId = @ItemId", new { ItemId = itemId });

            if (results.Count == 0)
            {
                throw new RepositoryException(
                    $"Item with ID {itemId} not found",
                    entityType: "InventoryItem",
                    entityId: itemId);
            }

            var row = results[0];
            return new InventoryItem
            {
                ItemId = (int)row["ItemId"],
                Name = (string)row["Name"],
                QuantityAvailable = (int)row["QuantityAvailable"],
                QuantityReserved = (int)row["QuantityReserved"]
            };
        }
        catch (RepositoryException)
        {
            throw; // Our own exception - let it propagate
        }
        catch (TimeoutException ex)
        {
            _logger.Error($"Database timeout while fetching item {itemId}", ex); // Log #1
            throw new RepositoryException($"Database timeout while fetching item {itemId}", ex,
                entityType: "InventoryItem", entityId: itemId);
        }
        catch (Exception ex)
        {
            _logger.Error($"Unexpected error fetching item {itemId}", ex); // Log #1
            throw new RepositoryException($"Failed to retrieve item {itemId} from database", ex,
                entityType: "InventoryItem", entityId: itemId);
        }
        finally
        {
            connection.Close();
        }
    }

    public void UpdateQuantity(int itemId, int newQuantityAvailable, int newQuantityReserved)
    {
        var connection = _connectionFactory.CreateConnection();

        try
        {
            connection.Open();
            var rowsAffected = connection.Execute(
                "UPDATE Inventory SET QuantityAvailable = @Available, QuantityReserved = @Reserved WHERE ItemId = @ItemId",
                new { ItemId = itemId, Available = newQuantityAvailable, Reserved = newQuantityReserved });

            if (rowsAffected == 0)
            {
                throw new RepositoryException($"No rows updated for item {itemId}",
                    entityType: "InventoryItem", entityId: itemId);
            }
        }
        catch (RepositoryException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to update quantity for item {itemId}", ex); // Log #1
            throw new RepositoryException($"Failed to update inventory for item {itemId}", ex,
                entityType: "InventoryItem", entityId: itemId);
        }
        finally
        {
            connection.Close();
        }
    }
}

public interface ILogger
{
    void Info(string message);
    void Warning(string message);
    void Error(string message, Exception? ex = null);
}

public class ConsoleLogger : ILogger
{
    private readonly string _category;
    public ConsoleLogger(string category) => _category = category;
    public void Info(string message) => Console.WriteLine($"[{_category}] INFO: {message}");
    public void Warning(string message) => Console.WriteLine($"[{_category}] WARN: {message}");
    public void Error(string message, Exception? ex = null) => 
        Console.WriteLine($"[{_category}] ERROR: {message}" + (ex != null ? $" - {ex.GetType().Name}: {ex.Message}" : ""));
}