namespace TaskManagement.Application.DTOs.Common;

public class ServiceResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public int? StatusCode { get; set; }
    
    public static ServiceResult<T> Success(T data) => new() 
    { 
        IsSuccess = true, 
        Data = data 
    };
    
    public static ServiceResult<T> Fail(string error, int statusCode = 400) => new() 
    { 
        IsSuccess = false, 
        Error = error,
        StatusCode = statusCode
    };
}