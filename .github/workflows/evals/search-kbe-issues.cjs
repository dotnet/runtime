"use strict";

const { execFile } = require("node:child_process");
const { promisify } = require("node:util");

const execFileAsync = promisify(execFile);

async function runGhApi(query, token, execFileImpl = execFileAsync) {
    const { stdout } = await execFileImpl("gh", [
        "api",
        "search/issues",
        "--method",
        "GET",
        "--field",
        `q=${query.trim()} repo:dotnet/runtime is:issue`,
        "--field",
        "per_page=10",
    ], {
        env: { ...process.env, GITHUB_TOKEN: token },
        maxBuffer: 1024 * 1024,
    });

    try {
        return JSON.parse(stdout);
    } catch {
        throw new Error("gh issue search returned invalid JSON");
    }
}

async function searchKbeIssues(query, token, runApi = runGhApi) {
    if (typeof query !== "string" || !query.trim()) {
        throw new Error("query must be a non-empty string");
    }
    if (!token) {
        throw new Error("a GitHub token must be set");
    }

    const result = await runApi(query.trim(), token);
    if (result.incomplete_results !== false || !Array.isArray(result.items)) {
        throw new Error("GitHub issue search returned an invalid response");
    }

    return result.items.map((item) => {
        if (
            item.repository_url !== "https://api.github.com/repos/dotnet/runtime" ||
            item.pull_request !== undefined ||
            !Number.isInteger(item.number) ||
            typeof item.user?.login !== "string" ||
            item.user.login.length === 0
        ) {
            throw new Error("GitHub issue search returned an invalid candidate");
        }

        return {
            number: item.number,
            user: { login: item.user.login },
        };
    });
}

module.exports = { runGhApi, searchKbeIssues };

if (require.main === module) {
    const token =
        process.env.GITHUB_TOKEN ||
        process.env.GH_TOKEN ||
        process.env.GITHUB_PERSONAL_ACCESS_TOKEN;

    searchKbeIssues(process.argv.slice(2).join(" "), token)
        .then((candidates) => console.log(JSON.stringify(candidates)))
        .catch((error) => {
            console.error(error.message);
            process.exitCode = 1;
        });
}
