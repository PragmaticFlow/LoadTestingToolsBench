import http from 'k6/http';
import { check, group } from 'k6';
import exec from 'k6/execution';

const BASE_URL = __ENV.BASE_URL || 'http://localhost:5000';
const USERS_PER_REQUEST = 20;
const VUs = 300;
const WARMUP_DURATION = '3s';
const DURATION = '3m';

export const options = {
    summaryTrendStats: ['p(50)', 'p(75)', 'p(95)', 'p(99)', 'avg', 'min', 'max'],
    thresholds: {
        'http_req_duration{group:::post_users}': [],
        'http_req_duration{group:::get_users}': [],
    },
    scenarios: {
        warmup: {
            executor: 'ramping-vus',
            startVUs: 0,
            stages: [{ duration: WARMUP_DURATION, target: VUs }],
        },
        users_scenario: {
            executor: 'constant-vus',
            vus: VUs,
            startTime: WARMUP_DURATION,
            duration: DURATION            
        },
    },
};

const users = Array.from({ length: USERS_PER_REQUEST }, (_, i) => ({
    id: i + 1,
    name: `User ${i + 1}`,
    email: `user${i + 1}@example.com`,
}));
const postHeaders = { 'Content-Type': 'application/json' };

export default function () {    

    group('post_users', function () {
        const payload = JSON.stringify(users);
        const res = http.post(`${BASE_URL}/api/users`, payload, { headers: postHeaders });
        check(res, { 'POST status 200': (r) => r.status === 200 });        
    });

    group('get_users', function () {
        const res = http.get(`${BASE_URL}/api/users`);
        check(res, { 'GET status 200': (r) => r.status === 200 });
    });
}