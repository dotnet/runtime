"use strict";

async function searchKbeIssues(query, token, fetchImpl = fetch) {
    if (typeof query !== "string" || !query.trim()) {
        throw new Error("query must be a non-empty string");
    }
    if (!token) {
        throw new Error("a GitHub token must be set");
    }

    const searchUrl = new URL("https://api.github.com/search/issues");
    searchUrl.searchParams.set("q", `${query.trim()} repo:dotnet/runtime is:issue`);
    searchUrl.searchParams.set("per_page", "10");

    const response = await fetchImpl(searchUrl, {
        headers: {
            Accept: "application/vnd.github+json",
            Authorization: `Bearer ${token}`,
            "X-GitHub-Api-Version": "2022-11-28",
        },
    });
    if (!response.ok) {
        throw new Error(`GitHub issue search failed with status ${response.status}`);
    }

    const result = await response.json();
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

module.exports = { searchKbeIssues };

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
