using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.WebSockets;
using WebSocket = NBomber.WebSockets.WebSocket;

public class WebSocketBenchmark
{
    const string WS_URL = "ws://localhost:5000/ws";
    const int USERS_PER_REQUEST = 20;
    const int VUs = 300;
    static readonly TimeSpan WARMUP_DURATION = TimeSpan.FromSeconds(3);
    static readonly TimeSpan DURATION = TimeSpan.FromMinutes(3);

    public void Run()
    {
        var users = Enumerable.Range(1, USERS_PER_REQUEST)
            .Select(i => new { Id = i, Name = $"User {i}", Email = $"user{i}@example.com" })
            .ToArray();

        // one persistent WebSocket per VU — connect once, then reuse for every iteration
        var sockets = new ConcurrentDictionary<int, WebSocket>();

        var scenario = Scenario.Create("ws_echo_scenario", async ctx =>
        {
            var id = ctx.ScenarioInfo.InstanceNumber;
            var ws = sockets.GetOrAdd(id, _ => new WebSocket(new WebSocketConfig()));

            if (ws.Client.State != WebSocketState.Open)
            {
                await Step.Run("connect", ctx, async () =>
                {
                    // a closed/errored WebSocket cancels its internal token and can't be reused —
                    // dispose it and start a fresh instance before reconnecting
                    if (ws.Client.State != WebSocketState.None)
                    {
                        ws.Dispose();
                        ws = new WebSocket(new WebSocketConfig());
                        sockets[id] = ws;
                    }

                    await ws.Connect(WS_URL);
                    return Response.Ok();
                });
            }

            await Step.Run("ping", ctx, async () =>
            {
                var usersJson = JsonSerializer.Serialize(users);
                await ws.Send(usersJson);
                return Response.Ok(sizeBytes: Encoding.UTF8.GetByteCount(usersJson));
            });

            await Step.Run("pong", ctx, async () =>
            {
                using var response = await ws.Receive(ctx.ScenarioCancellationToken);
                return Response.Ok(sizeBytes: response.Data.Length);
            });

            return Response.Ok();
        })
        .WithWarmUpDuration(TimeSpan.FromSeconds(2))
        .WithLoadSimulations(
            Simulation.RampingConstant(copies: VUs, during: WARMUP_DURATION),
            Simulation.KeepConstant(copies: VUs, during: DURATION)
        );

        NBomberRunner
            .RegisterScenarios(scenario)
            .Run();
    }
}
