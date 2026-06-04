using Microsoft.IO;
using System.Net.WebSockets;

var MsStreamManager = new RecyclableMemoryStreamManager();
const int BufferSize = 1024 * 16;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(o =>
    o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();
app.UseWebSockets();

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

app.Map("/ws", async (HttpContext context) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return;
    }

    using var webSocket = await context.WebSockets.AcceptWebSocketAsync();

    try
    {
        while (true)
        {
            using var ms = MsStreamManager.GetStream();
            ValueWebSocketReceiveResult result;
            do
            {
                var buffer = ms.GetMemory(BufferSize);
                result = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    return;
                }
                ms.Advance(result.Count);
            } while (!result.EndOfMessage);

            Interlocked.Increment(ref totalInvocations);
            if (ms.Length > 0)
            {
                var msg = ms.GetBuffer().AsMemory(0, (int)ms.Length);
                await webSocket.SendAsync(msg, WebSocketMessageType.Binary, WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
    }
});

app.Run();

record User(int Id, string Name, string Email);
