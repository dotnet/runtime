---
# Shared body for the build-failure-analysis workflows.
#
# Imported by build-failure-analysis.md (check_run + workflow_dispatch
# triggers) and build-failure-analysis-command.md (slash command). Keeps the
# prompt that drives the build-failure analysis in one place. Per-trigger
# wiring (steps, env, mcp-servers, permissions) lives in each caller because
# gh-aw merges those fields from imports but each main workflow must still
# re-declare its top-level permissions.

description: "Shared body for build-failure-analysis workflows"

# Callers must not override steps: gh-aw replaces that array rather than merging
# it. Keep metadata materialization and the final write guard together here.
safe-outputs:
  steps:
    - name: Ensure build-analysis output metadata
      if: steps.download-agent-output.outcome == 'success'
      uses: actions/github-script@v9.0.0
      env:
        GH_AW_AGENT_OUTPUT: ${{ steps.setup-agent-output-env.outputs.GH_AW_AGENT_OUTPUT }}
      with:
        script: |
          const fs = require("node:fs");
          const outputPath = process.env.GH_AW_AGENT_OUTPUT;
          const output = JSON.parse(fs.readFileSync(outputPath, "utf8"));
          if (!Array.isArray(output.items)) {
            throw new Error("Build-analysis output must contain an items array.");
          }
          for (const item of output.items) {
            if (item.type !== "add_comment" && item.type !== "create_pull_request_review_comment") {
              continue;
            }
            if (typeof item.body !== "string") {
              throw new Error("Build-analysis comments must have a string body.");
            }
            if (item.data === undefined) {
              item.data = { workflow_artifact: "build-failure-analysis", artifact_kind: "analysis" };
            } else if (item.data === null || Array.isArray(item.data) ||
                       Object.keys(item.data).length !== 2 ||
                       item.data.workflow_artifact !== "build-failure-analysis" ||
                       item.data.artifact_kind !== "analysis") {
              throw new Error("Build-analysis comment metadata does not match the workflow schema.");
            }
            // MCP normalizes supplied data into the body, but data is optional
            // at that boundary. Materialize the same block when it was omitted.
            const block = "Structured data:\n```json\n" + JSON.stringify(item.data, null, 2) + "\n```";
            if (!item.body.includes(block)) {
              item.body += "\n\n" + block;
            }
          }
          fs.writeFileSync(outputPath, JSON.stringify(output));
    - name: Revalidate PR revision before applying queued outputs
      shell: bash
      env:
        GH_TOKEN: ${{ github.token }}
        GH_AW_REPO: ${{ github.repository }}
        PR_NUMBER: ${{ needs.fetch-binlog.outputs.pr-number }}
        EXPECTED_HEAD: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
        EXPECTED_MERGE: ${{ needs.fetch-binlog.outputs.pr-merge-sha }}
        BUILD_ID: ${{ needs.fetch-binlog.outputs.ado-build-id }}
        ADO_API: "https://dev.azure.com/dnceng-public/public/_apis"
        ADO_BUILD_DEFINITION_ID: "129"
      run: |
        set -euo pipefail
        if [[ ! "${PR_NUMBER}" =~ ^[0-9]+$ || ! "${BUILD_ID}" =~ ^[0-9]+$ ]]; then
          echo "::error::Missing or invalid verified PR/build identity before applying outputs."
          exit 1
        fi
        # A rerun can succeed without changing either commit. Revalidate the
        # latest build as well as the revisions before publishing old failures.
        latest_build="${RUNNER_TEMP}/build-failure-analysis-latest-build.json"
        trap 'rm -f "${latest_build}"' EXIT
        if ! timeout 60 curl -sSL --fail --retry 3 --connect-timeout 10 --max-time 20 --retry-max-time 40 \
             -o "${latest_build}" \
             "${ADO_API}/build/builds?definitions=${ADO_BUILD_DEFINITION_ID}&branchName=refs/pull/${PR_NUMBER}/merge&queryOrder=queueTimeDescending&\$top=1&api-version=7.1" ||
           ! jq -e --arg id "${BUILD_ID}" \
             '.value[0] | (.id | tostring) == $id and .status == "completed" and .result == "failed"' \
             "${latest_build}" >/dev/null; then
          echo "::error::Analyzed build is no longer the latest completed failed runtime build, or could not be verified; refusing stale outputs."
          exit 1
        fi
        if [ -z "${EXPECTED_HEAD}" ] || [ -z "${EXPECTED_MERGE}" ] ||
           ! gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" |
             jq -e --arg head "${EXPECTED_HEAD}" --arg merge "${EXPECTED_MERGE}" \
               '.head.sha == $head and .merge_commit_sha == $merge' >/dev/null; then
          echo "::error::PR #${PR_NUMBER} moved or could not be verified before applying queued build-analysis outputs."
          exit 1
        fi
---

# Build Failure Analyst

You are the **build-failure analyst**. Analyze the binary logs of the Azure
DevOps build that just failed, plus its failed compile-task logs where Runtime
did not publish a matching binlog, and produce a PR review using the
safe-output tools (a later `safe_outputs` job performs the actual GitHub write).
Do **not** try to spawn a sub-agent: the `task` tool is intentionally not
available here. Work directly with the tools you do have: `binlog-mcp` to
read the logs, the `github` tools to read PR/repo context (the GitHub MCP
server is **read-only** here), the `safeoutputs` tools (`add_comment`,
`create_pull_request_review_comment`, `noop`) to post results, and a small set
of read-only `shell` commands (including `cat`).

## Instructions

1. Read the agent-context environment variables: `GH_AW_BUILD_OUTCOME`,
   `GH_AW_BINLOG_LIST`, `GH_AW_BINLOG_DIR`, `GH_AW_BINLOG_PATH`,
   `GH_AW_BINLOG_HOST_PATH`, `GH_AW_PR_NUMBER`, `GH_AW_PR_HEAD_SHA`,
   `GH_AW_PR_MERGE_SHA`, `GH_AW_WORKSPACE`.

2. If `GH_AW_BUILD_OUTCOME == 'success'`, the build did not actually fail —
   there is nothing to analyze. Call `noop` with the message
   `"Build succeeded — no analysis required."` and stop.

3. Load your detailed playbook: `cat .github/agents/build-failure-analyst.agent.md`
   (it is checked out with the repository config). Follow that methodology —
   root-cause grouping, source-context reading via the GitHub API at
   `GH_AW_PR_HEAD_SHA`, comment/suggestion formatting, and defensive behavior.
   In summary:
   - Start with `azdo_timeline` from the `hlx` MCP server for
     `GH_AW_BINLOG_HOST_PATH` using
     `filter: "failed"` to inventory **every** failed/canceled job and task.
     Treat only compile/build/configure/link tasks as build evidence; Helix,
     test execution, publishing, and infrastructure failures remain out of
     scope. For each failed compile task that is not explained by a retrieved
     binlog or by complete build diagnostics in its timeline `issues`, use
     `azdo_search_log` from `hlx` against that task's `logId` with bounded
     searches for compiler/MSBuild/native-build failure signatures. This is
     required even when some binlogs were retrieved: Runtime job display names
     and artifact names are not one-to-one, and some compile jobs publish no
     `Logs_Build_*` artifact.
   - Iterate **every** path in `GH_AW_BINLOG_LIST` when the list is non-empty
     (newline-separated in-container binlog paths from failed/canceled build
     jobs, under `GH_AW_BINLOG_DIR` = `/data/binlogs`) and query the
     `binlog-mcp` MCP server (`binlog_errors`, `binlog_overview`,
     `binlog_warnings`, …) with `binlog_file` set to each leg's path — a
     failure usually surfaces in only one leg, so do not analyse just the
     first. `binlog_errors`,
     `binlog_overview`, `binlog_warnings`, … are **MCP tools** provided by the
     `binlog-mcp` server: prefer calling them **directly as MCP tools** (with a
     `binlog_file` argument). A CLI wrapper is also mounted and allowlisted, so
     you may alternatively run `binlog-mcp <tool> --binlog_file <path>` via the
     shell.
   - If no binlog shows errors or failed-target/process evidence **and** the
     bounded hlx task-log checks show no compile/build failure, the build work
     compiled cleanly — the pipeline failure is then a **non-build**
     (test/Helix/publishing/infrastructure) failure, which is **out of scope**.
     Only make that clean-build conclusion when all required binlog and hlx
     queries succeeded. Then **post nothing**: call `noop` with a short reason
     and stop. Do **not** post a summary comment and do **not** invent fixes.
     If a required query fails and the gap prevents classification, post one
     incomplete-analysis summary with the Azure DevOps build link and no fix
     claim or inline suggestion.
   - Post exactly one summary via `add_comment` with structured data
     `{"workflow_artifact":"build-failure-analysis","artifact_kind":"analysis"}`
     and any inline
     `suggestion` blocks via `create_pull_request_review_comment`. Both
     workflows bind safe outputs deterministically to `GH_AW_PR_NUMBER`; do
     not attempt to choose or override the target in a safe-output call.
   - `submit_pull_request_review` is **not** a safe output for this workflow;
     inline comments stand alone.

4. When you have posted the analysis for a genuine build failure (or called
   `noop` for a clean-compile / non-build failure), stop.
