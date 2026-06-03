import json
from locust import HttpUser, SequentialTaskSet, LoadTestShape, task

USERS_PER_REQUEST = 20
VUS = 300
SPAWN_RATE = 60
DURATION = 3 * 60
WARM_UP = 3

USERS = [
    {"id": i, "name": f"User {i}", "email": f"user{i}@example.com"}
    for i in range(1, USERS_PER_REQUEST + 1)
]

class UsersScenario(SequentialTaskSet):
    @task
    def post_users(self):
        self.client.post(
            "/api/users",
            data=json.dumps(USERS),
            headers={"Content-Type": "application/json"},
        )

    @task
    def get_users(self):
        self.client.get("/api/users")

class UsersApiUser(HttpUser):
    tasks = [UsersScenario]

class StagesShape(LoadTestShape):
    def tick(self):
        t = self.get_run_time()
        if t < WARM_UP:
            ramp_users = max(1, round((t / WARM_UP) * VUS))
            return (ramp_users, SPAWN_RATE)
        elif t < WARM_UP + DURATION:
            return (VUS, SPAWN_RATE)
        return None
