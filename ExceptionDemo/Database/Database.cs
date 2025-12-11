using System.Data;

namespace OrderSystem.ExceptionDemo.Database;

/// <summary>
/// Simulates database operations that throw exceptions on failure.
/// </summary>
public class FakeDbConnection
{
    public void Open() { }
    public void Close() { }

    public int Execute(string sql, object? parameters = null)
    {
        // In a real scenario, this could throw:
        // - DataException ("Deadlock detected")
        // - TimeoutException ("Execution timeout expired")
        // - InvalidOperationException ("Connection closed unexpectedly")
        return 1;
    }

    public List<Dictionary<string, object>> Query(string sql, object? parameters = null)
    {
        return new List<Dictionary<string, object>>
        {
            new() { ["ItemId"] = 1, ["Name"] = "Widget", ["QuantityAvailable"] = 100, ["QuantityReserved"] = 5 }
        };
    }
}

public class DbConnectionFactory
{
    public FakeDbConnection CreateConnection() => new FakeDbConnection();
}