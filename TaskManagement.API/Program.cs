using TaskManagement.API.Extensions;
using TaskManagement.API.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwagger();

// Database
builder.Services.AddDatabase(builder.Configuration);

// Repositories
builder.Services.AddRepositories();

// Services
builder.Services.AddServices(builder.Configuration);

// Authentication
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<AuthMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();