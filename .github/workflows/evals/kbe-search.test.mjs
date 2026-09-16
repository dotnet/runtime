import assert from "node:assert/strict";
import { createRequire } from "node:module";
import { fileURLToPath } from "node:url";
import test from "node:test";

import { registerGraders } from "./kbe-candidate-reads-grader.mjs";

const require = createRequire(import.meta.url);
const { runGhApi, searchKbeIssues } = require("./search-kbe-issues.cjs");
const testToken = "test-token";
const trustedHelperPath = process.env.KBE_SEARCH_HELPER ??
    fileURLToPath(new URL("./search-kbe-issues.cjs", import.meta.url));
const trustedSearchCommand = `node "${trustedHelperPath}" query`;
const trustedWindowsSearchCommand =
    `node "${trustedHelperPath.replaceAll("/", "\\")}" query`;

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

test("eval search wrapper rejects incomplete results", async () => {
    const runApi = async () => ({ ...validResult(), incomplete_results: true });
    await assert.rejects(searchKbeIssues("query", testToken, runApi), /invalid response/);
});

test("eval search wrapper rejects malformed candidates", async () => {
    const malformed = validResult();
    malformed.items[0].user = {};
    const runApi = async () => malformed;
    await assert.rejects(searchKbeIssues("query", testToken, runApi), /invalid candidate/);
});

test("eval wrapper passes the bounded repository-scoped query to gh", async () => {
    let command;
    let args;
    let options;
    const execFileImpl = async (...received) => {
        [command, args, options] = received;
        return { stdout: JSON.stringify(validResult()) };
    };

    await runGhApi(" sample {owner} query ", testToken, execFileImpl);
    assert.equal(command, "gh");
    assert.deepEqual(args, [
        "api",
        "search/issues",
        "--method",
        "GET",
        "--raw-field",
        "q=sample {owner} query repo:dotnet/runtime is:issue",
        "--field",
        "per_page=10",
    ]);
    assert.equal(options.env.GITHUB_TOKEN, testToken);
    assert.equal(options.env.GH_TOKEN, testToken);
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
            command: trustedSearchCommand,
        }),
        result("bash", "search", {
            content: `Process exited with code 0\n${JSON.stringify([
                { number: 10, user: { login: "bot" } },
                { number: 20, user: { login: "user" } },
            ])}`,
        }),
        call("github-issue_read", "read-10", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("github-issue_read", "read-10", { number: 10 }),
        call("github-issue_read", "read-20", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 20,
        }),
        result("github-issue_read", "read-20", { number: 20 }),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, true);
});

test("candidate-read grader fails for filtered or missing candidate reads", async () => {
    const events = [
        call("powershell", "search", {
            command: trustedSearchCommand,
        }),
        result("powershell", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
            { number: 20, user: { login: "user" } },
        ])),
        call("github-issue_read", "read-10", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("github-issue_read", "read-10", "[Filtered]"),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.deepEqual(gradeResult.metadata.missing, [10, 20]);
});

test("candidate-read grader does not accept a read made before search results", async () => {
    const events = [
        call("mcp__github-issue_read", "read", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("mcp__github-issue_read", "read", { number: 10 }),
        call("bash", "search", {
            command: trustedSearchCommand,
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
        call("github-issue_read", "read-before", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("github-issue_read", "read-before", { number: 10 }),
        call("powershell", "search", {
            command: trustedWindowsSearchCommand,
        }),
        result("powershell", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
        ])),
        call("github-issue_read", "read-after", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: "10",
        }),
        result("github-issue_read", "read-after", { number: 10 }),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, true);
});

test("candidate-read grader rejects result-level issue read errors", async () => {
    const events = [
        call("bash", "search", {
            command: trustedSearchCommand,
        }),
        result("bash", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
        ])),
        call("github-issue_read", "read-10", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("github-issue_read", "read-10", { isError: true }),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.deepEqual(gradeResult.metadata.missing, [10]);
});

test("candidate-read grader rejects a read for the wrong issue", async () => {
    const events = [
        call("bash", "search", {
            command: trustedSearchCommand,
        }),
        result("bash", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
        ])),
        call("github-issue_read", "read-10", {
            owner: "dotnet", repo: "runtime", method: "get", issue_number: 10,
        }),
        result("github-issue_read", "read-10", { number: 11 }),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.deepEqual(gradeResult.metadata.missing, [10]);
});

test("candidate-read grader accepts searches with no candidates", async () => {
    const events = [
        call("bash", "search", {
            command: trustedSearchCommand,
        }),
        result("bash", "search", "[]"),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, true);
});

test("candidate-read grader fails for an unmatched search call", async () => {
    const events = [
        call("bash", "search", {
            command: trustedSearchCommand,
        }),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.match(gradeResult.evidence, /did not return a result/);
});

test("candidate-read grader fails closed on search errors", async () => {
    const events = [
        call("bash", "search", {
            command: trustedSearchCommand,
        }),
        result("bash", "search", "request failed", false),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.match(gradeResult.evidence, /search-kbe-issues call failed/);
});

test("candidate-read grader rejects a lookalike helper outside the trusted directory", async () => {
    const events = [
        call("bash", "search", {
            command: "node /tmp/search-kbe-issues.cjs query",
        }),
        result("bash", "search", JSON.stringify([
            { number: 10, user: { login: "bot" } },
        ])),
    ];

    const gradeResult = await grade(events);
    assert.equal(gradeResult.passed, false);
    assert.match(gradeResult.evidence, /search-kbe-issues was not called/);
});
