namespace OrderSystem.ResultDemo.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error? Error { get; }

    protected Result(bool isSuccess, Error? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success() => new(true, null);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string message, string code = "UNKNOWN") 
        => new(false, new Error(message, code));

    public Result Bind(Func<Result> next) => IsFailure ? this : next();
}

public class Result<T> : Result
{
    public T? Data { get; }

    private Result(bool isSuccess, T? data, Error? error) : base(isSuccess, error)
    {
        Data = data;
    }

    public static Result<T> Success(T data) => new(true, data, null);
    public new static Result<T> Failure(Error error) => new(false, default, error);
    public new static Result<T> Failure(string message, string code = "UNKNOWN") 
        => new(false, default, new Error(message, code));

    public Result<TNew> Map<TNew>(Func<T, TNew> mapper)
        => IsSuccess ? Result<TNew>.Success(mapper(Data!)) : Result<TNew>.Failure(Error!);

    public Result<TNew> Bind<TNew>(Func<T, Result<TNew>> next)
        => IsSuccess ? next(Data!) : Result<TNew>.Failure(Error!);

    public Result Bind(Func<T, Result> next)
        => IsSuccess ? next(Data!) : Result.Failure(Error!);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Error, TResult> onFailure)
        => IsSuccess ? onSuccess(Data!) : onFailure(Error!);
}

public class Error
{
    public string Message { get; }
    public string Code { get; }
    public Dictionary<string, object> Context { get; }
    public Error? InnerError { get; }

    public Error(string message, string code, Error? innerError = null)
    {
        Message = message;
        Code = code;
        Context = new Dictionary<string, object>();
        InnerError = innerError;
    }

    public Error WithContext(string key, object value)
    {
        Context[key] = value;
        return this;
    }

    public Error Wrap(string message, string code) => new Error(message, code, innerError: this);

    public override string ToString()
    {
        var ctx = Context.Count > 0 ? $" [{string.Join(", ", Context.Select(kv => $"{kv.Key}={kv.Value}"))}]" : "";
        var inner = InnerError != null ? $" -> {InnerError}" : "";
        return $"[{Code}] {Message}{ctx}{inner}";
    }
}

public static class ErrorCodes
{
    public const string DatabaseTimeout = "DB_TIMEOUT";
    public const string DatabaseConnection = "DB_CONNECTION";
    public const string DatabaseDeadlock = "DB_DEADLOCK";
    public const string DatabaseError = "DB_ERROR";
    
    public const string EntityNotFound = "ENTITY_NOT_FOUND";
    public const string UpdateFailed = "UPDATE_FAILED";
    
    public const string InsufficientInventory = "INSUFFICIENT_INVENTORY";
    public const string ItemNotFound = "ITEM_NOT_FOUND";
    public const string ReservationFailed = "RESERVATION_FAILED";
    
    public const string OrderFailed = "ORDER_FAILED";
    public const string ValidationError = "VALIDATION_ERROR";
}