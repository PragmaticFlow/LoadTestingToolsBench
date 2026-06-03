# LoadTesting Tools Bench

Benchmarks comparing popular load testing tools against a common ASP.NET Core target API.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [k6 v2.0.0](https://grafana.com/docs/k6/latest/set-up/install-k6/)
- Python 3 + Locust 2.44.1 — `pip install locust==2.44.1`
- Java 25 + Maven (for Gatling)

## Project Structure

```
src/
  TargetApi/          # ASP.NET Core minimal API under test
tests/
  NBomber/            # NBomber load test (.NET)
  k6/                 # k6 load test (JavaScript)
  Locust/             # Locust load test (Python)
  Gatling/            # Gatling load test (Java/Maven)
```

## Target API

Runs on `http://localhost:5000` and exposes three endpoints:

| Method | Path | Description |
|--------|------|-------------|
| `POST` | `/api/users` | Store a batch of users |
| `GET` | `/api/users` | Retrieve stored users |
| `GET` | `/api/stats` | Total invocation count and user count |

## How to Run the Benchmarks

### Prepare Environment

Increase the ephemeral port range on Windows (run as Admin):
```
netsh int ipv4 set dynamicport tcp start=1024 num=64511
```

### Start the Target API

```
dotnet run --project src/TargetApi/TargetApi.csproj -c Release
```

### Run a Load Test

**NBomber**
```
dotnet run --project tests/NBomber/LoadTests.NBomber.csproj -c Release
```

**k6**
```
k6 run tests/k6/script.js
```

**Locust**
```
locust -f tests/Locust/locustfile.py --headless --host http://localhost:5000
```

**Gatling**
```
cd tests/Gatling
mvn gatling:test
```
