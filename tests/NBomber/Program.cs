using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;
using NBomber.CSharp;
using NBomber.Http.CSharp;

const string BASE_URL = "http://localhost:5000";
const int USERS_PER_REQUEST = 20;
const int VUs = 300;
TimeSpan WARMUP_DURATION = TimeSpan.FromSeconds(3);
TimeSpan DURATION = TimeSpan.FromMinutes(3);

var users = Enumerable.Range(1, USERS_PER_REQUEST)
    .Select(i => new { Id = i, Name = $"User {i}", Email = $"user{i}@example.com" })
    .ToArray();

// var sharedHttpClient = Http.CreateDefaultClient();
// HttpClient GetClient(int vuId) => sharedHttpClient;

// one HttpClient per VU — each has its own SocketsHttpHandler and connection pool
var perVuClients = new ConcurrentDictionary<int, HttpClient>();
HttpClient GetClient(int vuId) => perVuClients.GetOrAdd(vuId, _ => 
    new HttpClient(new SocketsHttpHandler
    {
        MaxConnectionsPerServer = 1,
        PooledConnectionLifetime = Timeout.InfiniteTimeSpan,
    }));

var scenario = Scenario.Create("users_scenario", async context =>
{
    var httpClient = GetClient(context.ScenarioInfo.InstanceNumber);
    
    var post = await Step.Run("post_users", context, async () =>
    {
        var usersJson = JsonSerializer.Serialize(users);
        
        var request = Http.CreateRequest("POST", $"{BASE_URL}/api/users")
            .WithBody(new StringContent(usersJson, System.Text.Encoding.UTF8, "application/json"));
        
        return await Http.Send(httpClient, request);
    });

    var get = await Step.Run("get_users", context, async () =>
    {
        var request = Http.CreateRequest("GET", $"{BASE_URL}/api/users");
        var response = await Http.Send(httpClient, request);
        return response;
    });

    return Response.Ok();
})
.WithWarmUpDuration(TimeSpan.FromSeconds(2))
.WithLoadSimulations(
    // Simulation.Inject(rate: RATE, interval: TimeSpan.FromSeconds(1), during: DURATION)
    Simulation.RampingConstant(copies: VUs, during: WARMUP_DURATION),
    Simulation.KeepConstant(copies: VUs, during: DURATION)
);

NBomberRunner
    .RegisterScenarios(scenario)
    .Run();