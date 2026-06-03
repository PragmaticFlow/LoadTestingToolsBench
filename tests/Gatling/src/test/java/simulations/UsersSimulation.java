package simulations;

import com.fasterxml.jackson.databind.ObjectMapper;
import io.gatling.javaapi.core.*;
import io.gatling.javaapi.http.*;

import java.time.Duration;
import java.util.List;
import java.util.stream.IntStream;

import static io.gatling.javaapi.core.CoreDsl.*;
import static io.gatling.javaapi.http.HttpDsl.*;

public class UsersSimulation extends Simulation {

    private static final String BASE_URL =
        System.getenv().getOrDefault("BASE_URL", "http://localhost:5000");

    private static final int USERS_PER_REQUEST = 20;
    private static final int VUs = 300;
    private static final Duration WARM_UP_DURATION = Duration.ofSeconds(3);
    private static final Duration HOLD_DURATION = Duration.ofMinutes(3);
    private static final ObjectMapper objectMapper = new ObjectMapper();

    record User(int id, String name, String email) {}

    private static final List<User> USERS = IntStream.rangeClosed(1, USERS_PER_REQUEST)
        .mapToObj(i -> new User(i, "User " + i, "user" + i + "@example.com"))
        .toList();

    HttpProtocolBuilder httpProtocol = http
        .baseUrl(BASE_URL)
        .acceptHeader("application/json")
        .contentTypeHeader("application/json");

    ScenarioBuilder usersScenario = scenario("users_scenario")
        .exec(
            http("post_users")
                .post("/api/users")
                .body(StringBody(session -> {
                    try {
                        return objectMapper.writeValueAsString(USERS);
                    } catch (com.fasterxml.jackson.core.JsonProcessingException e) {
                        throw new RuntimeException(e);
                    }
                }))
                .check(status().is(200))
        )
        .exec(
            http("get_users")
                .get("/api/users")
                .check(status().is(200))
        );

    {
        setUp(
            usersScenario.injectClosed(
                rampConcurrentUsers(0).to(VUs).during(WARM_UP_DURATION),
                constantConcurrentUsers(VUs).during(HOLD_DURATION)
            )
        ).protocols(httpProtocol);
    }
}
