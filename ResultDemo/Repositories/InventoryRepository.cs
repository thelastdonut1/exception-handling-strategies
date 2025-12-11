using OrderSystem.ResultDemo.Common;
using OrderSystem.ResultDemo.Database;
using OrderSystem.Models;

namespace OrderSystem.ResultDemo.Repositories;

/// <summary>
/// No try-catch blocks. No logging. Errors flow up via Result type.
/// </summary>
public class InventoryRepository
{
    private readonly DbConnectionFactory _connectionFactory;

    public InventoryRepository(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public Result<InventoryItem> GetItem(int itemId)
    {
        var connection = _connectionFactory.CreateConnection();

        var openResult = connection.Open();
        if (openResult.IsFailure)
        {
            connection.Close();
            return Result<InventoryItem>.Failure(
                openResult.Error!.Wrap($"Failed to connect while fetching item {itemId}", ErrorCodes.DatabaseConnection));
        }

        var queryResult = connection.Query("SELECT * FROM Inventory WHERE ItemId = @ItemId", new { ItemId = itemId });
        connection.Close();

        if (queryResult.IsFailure)
        {
            return Result<InventoryItem>.Failure(
                queryResult.Error!.WithContext("ItemId", itemId));
        }

        var results = queryResult.Data!;
        if (results.Count == 0)
        {
            return Result<InventoryItem>.Failure(
                new Error($"Item with ID {itemId} not found", ErrorCodes.EntityNotFound)
                    .WithContext("ItemId", itemId));
        }

        var row = results[0];
        return Result<InventoryItem>.Success(new InventoryItem
        {
            ItemId = (int)row["ItemId"],
            Name = (string)row["Name"],
            QuantityAvailable = (int)row["QuantityAvailable"],
            QuantityReserved = (int)row["QuantityReserved"]
        });
    }

    public Result UpdateQuantity(int itemId, int newQuantityAvailable, int newQuantityReserved)
    {
        var connection = _connectionFactory.CreateConnection();

        var openResult = connection.Open();
        if (openResult.IsFailure)
        {
            connection.Close();
            return Result.Failure(openResult.Error!.WithContext("ItemId", itemId));
        }

        var executeResult = connection.Execute(
            "UPDATE Inventory SET QuantityAvailable = @Available, QuantityReserved = @Reserved WHERE ItemId = @ItemId",
            new { ItemId = itemId, Available = newQuantityAvailable, Reserved = newQuantityReserved });
        connection.Close();

        if (executeResult.IsFailure)
        {
            return Result.Failure(executeResult.Error!.WithContext("ItemId", itemId));
        }

        if (executeResult.Data == 0)
        {
            return Result.Failure(
                new Error($"No rows updated for item {itemId}", ErrorCodes.UpdateFailed)
                    .WithContext("ItemId", itemId));
        }

        return Result.Success();
    }
}