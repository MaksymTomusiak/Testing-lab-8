import http from 'k6/http';
import { check, sleep } from 'k6';

export const options = {
    stages: [
        { duration: '20s', target: 100 },
        { duration: '20s', target: 200 },
        { duration: '20s', target: 500 },
        { duration: '20s', target: 0 },
    ],
    thresholds: {
        http_req_failed: ['rate<0.05'],
    },
};

export default function () {
    const baseUrl = __ENV.BASE_URL || 'http://localhost:8080';
    http.get(`${baseUrl}/api/products/search?q=Widget`);
    sleep(1);
}
