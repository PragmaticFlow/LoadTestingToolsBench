var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();

User[]? users = null;

long totalInvocations = 0;

app.MapGet("/api/users", () =>
{
    Interlocked.Increment(ref totalInvocations);
    return Results.Ok(users);
});

app.MapPost("/api/users", (User[] incoming) =>
{
    Interlocked.CompareExchange(ref users, incoming, null);
    Interlocked.Increment(ref totalInvocations);
    return Results.Ok();
});

app.MapGet("/api/stats", () => 
    Results.Ok(new { 
        TotalInvocations = Interlocked.Read(ref totalInvocations),
        UsersCount = users?.Length ?? 0
    }));

app.Run();

record User(int Id, string Name, string Email);
