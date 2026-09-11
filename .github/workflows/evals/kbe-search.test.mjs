import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { createRequire } from "node:module";
import test from "node:test";

import { registerGraders } from "./kbe-candidate-reads-grader.mjs";

const require = createRequire(import.meta.url);
const { searchKbeIssues } = require("./search-kbe-issues.cjs");
const testToken = "test-token";

async function productionSearch() {
    const workflow = await readFile(new URL("../ci-failure-scan.md", import.meta.url), "utf8");
    const scriptMatch = workflow.match(
        /^  search-kbe-issues:\r?\n[\s\S]*?^    script: \|\r?\n(?<script>(?:^      .*(?:\r?\n|$))+?)^    env:/m
    );
    assert.ok(scriptMatch?.groups?.script, "production search-kbe-issues script was not found");
    const script = scriptMatch.groups.script
        .split(/\r?\n/)
        .map((line) => line.slice(6))
        .join("\n");

    return new Function("query", "fetch", "process", `return (async () => {\n${script}\n})();`);
}

function response(body, { ok = true, status = 200 } = {}) {
    return {
        ok,
        status,
        json: async () => body,
    };
}

function validResult() {
    return {
        incomplete_results: false,
        items: [{
            repository_url: "https://api.github.com/repos/dotnet/runtime",
            number: 132843,
            user: { login: "dotnet-bot" },
        }],
    };
}

test("production and eval search wrappers return the same inert candidates", async () => {
    const production = await productionSearch();
    const fetchImpl = async (url) => {
        assert.equal(url.searchParams.get("q"), "sample query repo:dotnet/runtime is:issue");
        assert.equal(url.searchParams.get("per_page"), "10");
        return response(validResult());
    };
    const processStub = { env: { GITHUB_TOKEN: testToken } };

    assert.deepEqual(
        await production(" sample query ", fetchImpl, processStub),
        await searchKbeIssues(" sample query ", testToken, fetchImpl)
    );
});

test("production and eval search wrappers reject incomplete results", async () => {
    const production = await productionSearch();
    const fetchImpl = async () => response({ ...validResult(), incomplete_results: true });
    const processStub = { env: { GITHUB_TOKEN: testToken } };

    await assert.rejects(production("query", fetchImpl, processStub), /invalid response/);
    await assert.rejects(searchKbeIssues("query", testToken, fetchImpl), /invalid response/);
});

test("production and eval search wrappers reject malformed candidates", async () => {
    const production = await productionSearch();
    const malformed = validResult();
    malformed.items[0].user = {};
    const fetchImpl = async () => response(malformed);
    const processStub = { env: { GITHUB_TOKEN: testToken } };

    await assert.rejects(production("query", fetchImpl, processStub), /invalid candidate/);
    await assert.rejects(searchKbeIssues("query", testToken, fetchImpl), /invalid candidate/);
});

function trajectory(events) {
    return {
        id: "test",
        stimulus: {},
        events,
        metrics: {},
        output: "",
        workDir: "",
        metadata: {},
    };
}

function call(toolName, toolCallId, args) {
    return {
        type: "tool_call",
        data: { toolName, toolCallId, arguments: args },
    };
}

function result(toolName, toolCallId, value, success = true) {
    return {
        type: "tool_result",
        data: { toolName, toolCallId, result: value, success },
    };
}

async function grade(events) {
    let grader;
    registerGraders({
        register(value) {
            grader = value;
        },
    });
    return grader.grade({ trajectory: trajectory(events) });
}

test("candidate-read grader requires successful unfiltered reads for every candidate", async () => {
    const events = [
        call("bash", "search", {
            command: "node .github/workflows/evals/search-kbe-issues.cjs query",
        }),
        result("bash", "search", {
            content: `Process exited with code 0\n${JSON.stringify([
                { number: 10, user: { login: "bot" } },
                { number: 20, user: { login: "user" } },
            ])}`,
        }),
        call("issue_read", "read-10", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("issue_read", "read-10", { number: 10 }),
        call("issue_read", "read-20", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 20,
        }),
        result("issue_read", "read-20", { number: 20 }),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, true);
});

test("candidate-read grader fails for filtered or missing candidate reads", async () => {
    const events = [
        call("powershell", "search", {
            command: "node .github/workflows/evals/search-kbe-issues.cjs query",
        }),
        result("powershell", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
            { number: 20, user: { login: "user" } },
        ])),
        call("github.issue_read", "read-10", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("github.issue_read", "read-10", "[Filtered]"),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.deepEqual(gradeResult.metadata.missing, [10, 20]);
});

test("candidate-read grader does not accept a read made before search results", async () => {
    const events = [
        call("issue_read", "read", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("issue_read", "read", { number: 10 }),
        call("bash", "search", {
            command: "node .github/workflows/evals/search-kbe-issues.cjs query",
        }),
        result("bash", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
        ])),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.deepEqual(gradeResult.metadata.missing, [10]);
});

test("candidate-read grader accepts a repeated read after search results", async () => {
    const events = [
        call("issue_read", "read-before", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("issue_read", "read-before", { number: 10 }),
        call("powershell", "search", {
            command: "node .\\.github\\workflows\\evals\\search-kbe-issues.cjs query",
        }),
        result("powershell", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
        ])),
        call("issue_read", "read-after", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: "10",
        }),
        result("issue_read", "read-after", { number: 10 }),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, true);
});

test("candidate-read grader accepts searches with no candidates", async () => {
    const events = [
        call("bash", "search", {
            command: "node .github/workflows/evals/search-kbe-issues.cjs query",
        }),
        result("bash", "search", "[]"),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, true);
});

test("candidate-read grader fails closed on search errors", async () => {
    const events = [
        call("bash", "search", {
            command: "node .github/workflows/evals/search-kbe-issues.cjs query",
        }),
        result("bash", "search", "request failed", false),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.match(gradeResult.evidence, /search-kbe-issues call failed/);
});
