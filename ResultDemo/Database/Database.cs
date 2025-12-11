using OrderSystem.ResultDemo.Common;

namespace OrderSystem.ResultDemo.Database;

/// <summary>
/// Simulates database operations that return Results instead of throwing.
/// </summary>
public class FakeDbConnection
{
    public Result Open()
    {
        // Could return failure:
        // Result.Failure("Connection unavailable", ErrorCodes.DatabaseConnection)
        return Result.Success();
    }

    public void Close() { }

    public Result<int> Execute(string sql, object? parameters = null)
    {
        // Could return various failures:
        // Result<int>.Failure("Deadlock detected", ErrorCodes.DatabaseDeadlock)
        // Result<int>.Failure("Timeout expired", ErrorCodes.DatabaseTimeout)
        return Result<int>.Success(1);
    }

    public Result<List<Dictionary<string, object>>> Query(string sql, object? parameters = null)
    {
        var data = new List<Dictionary<string, object>>
        {
            new() { ["ItemId"] = 1, ["Name"] = "Widget", ["QuantityAvailable"] = 100, ["QuantityReserved"] = 5 }
        };
        return Result<List<Dictionary<string, object>>>.Success(data);
    }
}

public class DbConnectionFactory
{
    public FakeDbConnection CreateConnection() => new FakeDbConnection();
}