using System.Security.Claims;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.API.Middleware;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    
    public AuthMiddleware(RequestDelegate next)
    {
        _next = next;
    }
    
    public async Task InvokeAsync(HttpContext context, IJwtService jwtService)
    {
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        
        if (token != null)
        {
            var principal = jwtService.ValidateAccessToken(token);
            if (principal != null)
            {
                context.User = principal;
            }
        }
        
        await _next(context);
    }
}