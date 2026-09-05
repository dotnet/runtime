---
name: "Build Failure Analysis"
description: >-
  When the Azure Pipelines PR build (`runtime`) fails, downloads the binary
  logs from its failed or canceled jobs — it does NOT rebuild — and delegates
  to the `build-failure-analyst` agent. The agent queries binlogs via
  `binlog-mcp` and uses `hlx` to inspect failed Azure DevOps compile-task logs
  when Runtime did not publish a matching binlog artifact.

# This workflow is **advisory**, not gating, and it performs **no build of its
# own**. Runtime's authoritative PR build runs on Azure DevOps
# (dnceng-public/public, pipeline "runtime", definitionId 129) and publishes
# each build job's binary log in a `Logs_Build_<leg>` pipeline artifact. When
# that build's GitHub check reports failure, this workflow uses the Azure
# DevOps timeline to select the artifacts for failed or canceled jobs
# (anonymously — dnceng-public/public is a public project), then the agent
# analyses whichever selected leg(s) contain errors. Reusing the binlogs avoids
# a duplicate build: the analysis pipeline only downloads build artifacts
# (data) and reads them — it does **not** build or execute PR code. (gh-aw's
# generated agent job **does** check out the repository — via
# `actions/checkout` — to load the workflow's own agent configuration; that
# checkout is for tooling only and uses the event's ref, **not** the PR head,
# so no PR code is built or executed.)

on:
  # `check_run` fires for every check on a commit, so the `fetch-binlog` job
  # below filters tightly to the `runtime` build check reporting failure.
  check_run:
    types: [completed]
  # Advisory analysis should run for **every** failing PR — including external
  # contributors' PRs, which are the most likely to break the build. Disable
  # gh-aw's default author-association gate (which would otherwise skip
  # non-write-access actors, and on `check_run` the actor is the pipeline app
  # anyway). This is safe here: the workflow only reads a public binlog and
  # posts advisory comments — it never builds or executes PR code.
  roles: all
  # Manual entry point for reruns / testing: analyse a specific Azure DevOps
  # build id and post to a specific PR.
  workflow_dispatch:
    inputs:
      ado-build-id:
        description: "Azure DevOps build id to analyze (dnceng-public/public)."
        required: true
        type: string
      pr-number:
        description: "PR number to post the analysis on."
        required: true
        type: string
  # Gate the whole AI pipeline on the fetch job so the agent only runs after the
  # failed Azure DevOps build and target revision have been verified.
  needs: [fetch-binlog]

# A Runtime compile failure does not always publish a `Logs_Build_*` artifact.
# Activate once the failed build is verified; `binlog-mcp` handles available
# binlogs and `hlx` provides the bounded Azure DevOps task-log fallback.
if: needs.fetch-binlog.outputs.analysis-ready == 'true'

# Least-privilege for the workflow/agent jobs. The agent runs read-only; it
# does NOT post directly. All PR writes (summary comment + inline review
# suggestions) go through gh-aw **safe-outputs**, which the compiler emits as
# a separate `safe_outputs` job granted `pull-requests: write` + `issues:
# write` in the generated lock. Keep `pull-requests: read` here so the AI
# agent job stays least-privilege — do NOT raise it to `write`, that would
# hand PR-write scope to the agent job unnecessarily.
#
# Do NOT add `copilot-requests: write` here. That permission switches gh-aw's
# generated lock from `COPILOT_GITHUB_TOKEN: ${{ secrets.COPILOT_GITHUB_TOKEN }}`
# to `${{ github.token }}`, and the ephemeral Actions token is not entitled for
# inference against api.githubcopilot.com in this org — every agent run then
# dies in ~2s with "Authentication failed with provider ... (HTTP 403)" on both
# /models and /chat/completions, before it reads the prompt or opens a binlog.
# `update-default-versions.md` omits it and works; keep this consistent.
permissions:
  contents: read
  pull-requests: read

concurrency:
  # Only real `runtime` check_run events (and manual dispatch for a PR) use a
  # PR/head-scoped group, so a newer analysis supersedes an in-progress one for
  # the same PR. Every OTHER completed check_run on the PR would otherwise land
  # in the same group and — with cancel-in-progress — abort the running real
  # analysis, so those get a unique per-run group that collides with nothing.
  group: ${{ (github.event_name == 'check_run' && github.event.check_run.name == 'runtime' && format('build-failure-analysis-{0}', github.event.check_run.pull_requests[0].number || github.event.check_run.head_sha)) || (github.event_name == 'workflow_dispatch' && format('build-failure-analysis-{0}', inputs['pr-number'])) || format('build-failure-analysis-run-{0}', github.run_id) }}
  cancel-in-progress: true

timeout-minutes: 30

network:
  allowed:
    - defaults
    - dotnet
    - dev.azure.com

# ###############################################################
# Select a PAT from the pool and override COPILOT_GITHUB_TOKEN.
# Run agentic jobs in an isolated `copilot-pat-pool` environment.
#
# When org-level billing is available, this will be removed.
# See `shared/pat_pool.README.md` for more information.
# ###############################################################
imports:
  - uses: shared/pat_pool.md
    with:
      environment: copilot-pat-pool
  - shared/build-failure-analysis-shared.md

environment: copilot-pat-pool

engine:
  id: copilot
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, 'NO COPILOT PAT AVAILABLE') }}

# Live binlog access for the agent. The build-leg binlogs are downloaded from
# Azure DevOps by the fetch-binlog job into a directory, uploaded as an
# artifact, downloaded by the agent job to `/tmp/binlogs`, and mounted
# read-only into this container at `/data/binlogs` by the gh-aw MCP gateway.
#
# The digest is pinned in `.github/aw/actions-lock.json` because this container
# processes artifacts from untrusted PRs. Refresh/inspect the current digest with:
#   docker buildx imagetools inspect \
#     mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64
mcp-servers:
  binlog-mcp:
    container: "mcr.microsoft.com/dotnet-buildtools/prereqs:azurelinux-3.0-binlog-mcp-amd64"
    mounts:
      - "/tmp/binlogs:/data/binlogs:ro"
    allowed: ["binlog_*"]
  # Runtime build/test jobs do not always publish a matching build-log
  # artifact. Keep binlog-mcp as the primary analyzer, but let the agent query
  # the verified public Azure DevOps build's failed compile-task logs when a
  # binlog is absent. Only build-log tools are exposed; Helix/test diagnostics
  # remain outside this workflow's scope.
  hlx:
    container: "ghcr.io/lewing/helix.mcp:v0.8.0"
    env:
      AZDO_TOKEN: ""
      HELIX_ACCESS_TOKEN: ""
      HLX_CACHE_MAX_SIZE_MB: "256"
    allowed:
      - "azdo_timeline"
      - "azdo_search_timeline"
      - "azdo_search_log"
      - "azdo_log"
      - "azdo_artifacts"

# Custom job that reuses the binlogs from the failed Azure DevOps build instead
# of rebuilding. It resolves the ADO build id (from the check details URL or
# the dispatch input), verifies the PR targets an in-scope base branch,
# selects `Logs_Build_*` artifacts matching failed or canceled timeline jobs,
# extracts each selected leg's `*.binlog`, and uploads them for the agent job.
jobs:
  fetch-binlog:
    name: Fetch binlogs (Azure Pipelines)
    runs-on: ubuntu-latest
    timeout-minutes: 15
    # `check_run` fires for every check; only act on the Runtime PR build check
    # reporting failure (or a manual dispatch).
    if: >
      github.event_name == 'workflow_dispatch' ||
      (github.event.check_run.name == 'runtime' && github.event.check_run.conclusion == 'failure')
    permissions:
      contents: read
      pull-requests: read
    outputs:
      analysis-ready: ${{ steps.fetch.outputs.analysis-ready }}
      binlog-found: ${{ steps.fetch.outputs.binlog-found }}
      pr-number: ${{ steps.fetch.outputs.pr-number }}
      pr-head-sha: ${{ steps.fetch.outputs.pr-head-sha }}
      pr-merge-sha: ${{ steps.fetch.outputs.pr-merge-sha }}
      ado-build-id: ${{ steps.fetch.outputs.ado-build-id }}
      ado-build-url: ${{ steps.fetch.outputs.ado-build-url }}
    steps:
      - name: Download binlogs from the failed Azure Pipelines build
        id: fetch
        shell: bash
        env:
          GH_TOKEN: ${{ github.token }}
          GH_AW_REPO: ${{ github.repository }}
          ADO_API: "https://dev.azure.com/dnceng-public/public/_apis"
          ADO_BUILD_UI: "https://dev.azure.com/dnceng-public/public/_build/results"
          # runtime pipeline definition id in dnceng-public/public (used to
          # validate a dispatched build id belongs to the right pipeline).
          ADO_BUILD_DEFINITION_ID: "129"
          EVENT_NAME: ${{ github.event_name }}
          CHECK_DETAILS_URL: ${{ github.event.check_run.details_url }}
          CHECK_HEAD_SHA: ${{ github.event.check_run.head_sha }}
          CHECK_PR_NUMBER: ${{ github.event.check_run.pull_requests[0].number }}
          DISPATCH_BUILD_ID: ${{ inputs['ado-build-id'] }}
          DISPATCH_PR_NUMBER: ${{ inputs['pr-number'] }}
        run: |
          # Advisory + fail-closed: on any validation gap keep the agent inert.
          set +e
          set +o pipefail
          # A set but unwritable path would pass a non-empty check and then
          # fail on every append, leaving the step with no outputs at all
          # instead of the intended controlled no-op. Probe with a zero-byte
          # append, which verifies writability without adding content.
          if [ -z "${GITHUB_OUTPUT}" ] || ! printf '' >> "${GITHUB_OUTPUT}" 2>/dev/null; then
            echo "::error::GITHUB_OUTPUT is unset or not writable; refusing to run without a way to emit step outputs." >&2
            exit 1
          fi
          emit_none() {
            {
              echo "analysis-ready=false"
              echo "binlog-found=false"
            } >> "$GITHUB_OUTPUT"
            exit 0
          }

          # Fetch an Azure DevOps API document into ADO_DOC. A network failure
          # or a non-JSON body is a data-resolution failure, not evidence that
          # there is nothing to analyze, so it is reported as such instead of
          # falling through to an empty `.value` and a misleading warning.
          # Returns a status rather than calling emit_none directly, because a
          # call in a command substitution would only exit the subshell.
          ado_get() {
            local what="$1" url="$2" rc tmp
            # `mktemp` rather than a fixed /tmp name: a predictable path is one
            # pre-created symlink -- or one collision with another job sharing the
            # runner -- away from being someone else's file.
            tmp=$(mktemp) || {
              echo "::warning::Could not create a temporary file for the ${what}; treating as a data-resolution failure."
              return 1
            }
            # These are small JSON documents; cap them so a stalled endpoint
            # fails in seconds rather than hanging the job until its overall
            # timeout. The artifact download below sets its own, much larger,
            # budget.
            # Write to a file rather than capturing stdout: `curl --retry` can only
            # rewind seekable output, and command-substitution stdout is a pipe. A
            # retry after a partial or error body would append to it, so a *successful*
            # retry would yield two concatenated documents, `jq` would reject them, and
            # the run would be reported as a data-resolution failure. With `-o` curl
            # truncates the file before each attempt, so only the last response
            # survives.
            timeout 60 curl -sSL --fail --retry 3 --connect-timeout 10 --max-time 20 --retry-max-time 40 -o "${tmp}" "${url}"
            rc=$?
            ADO_DOC=$(cat "${tmp}" 2>/dev/null)
            rm -f "${tmp}"
            if [ "${rc}" -ne 0 ] || [ -z "${ADO_DOC}" ]; then
              echo "::warning::Could not fetch the ${what} from Azure DevOps (curl exit ${rc}); treating as a data-resolution failure."
              return 1
            fi
            if ! printf '%s' "${ADO_DOC}" | jq -e . >/dev/null 2>&1; then
              echo "::warning::Azure DevOps returned a non-JSON ${what}; treating as a data-resolution failure."
              return 1
            fi
            return 0
          }

          # --- 1. Resolve the Azure DevOps build id ---
          if [ "${EVENT_NAME}" = "workflow_dispatch" ]; then
            BUILD_ID="${DISPATCH_BUILD_ID}"
          else
            # details_url looks like: .../_build/results?buildId=NNN&view=...
            BUILD_ID=$(printf '%s' "${CHECK_DETAILS_URL}" | grep -oE 'buildId=[0-9]+' | head -1 | cut -d= -f2)
          fi
          [ -z "${BUILD_ID}" ] && { echo "::warning::Could not resolve an ADO build id."; emit_none; }
          # The build id feeds directly into ADO API URLs below; require it to
          # be purely numeric (esp. on workflow_dispatch, where it is free-form
          # input) so a malformed value can't alter the request path/query.
          if ! printf '%s' "${BUILD_ID}" | grep -qE '^[0-9]+$'; then
            echo "::warning::Resolved ADO build id is not numeric; refusing."; emit_none
          fi
          echo "Azure DevOps build id: '${BUILD_ID}'"

          # Fetch the build metadata once, up front: it is the authoritative
          # source for the definition/result/revision validated in step 4.
          # The PR number remains event-owned so safe outputs can be bound to
          # the same trusted value before the fetch job runs.
          ado_get "build metadata" "${ADO_API}/build/builds/${BUILD_ID}?api-version=7.1" || emit_none
          build_json="${ADO_DOC}"
          RESULT=$(printf '%s' "${build_json}" | jq -r '.result // empty')
          DEF_ID=$(printf '%s' "${build_json}" | jq -r '.definition.id // empty')
          SRC_BRANCH=$(printf '%s' "${build_json}" | jq -r '.sourceBranch // empty')

          # --- 2. Resolve the PR number + head SHA ---
          if [ "${EVENT_NAME}" = "workflow_dispatch" ]; then
            PR_NUMBER="${DISPATCH_PR_NUMBER}"
            HEAD_SHA=""
          else
            # Safe outputs are bound to check_run.pull_requests[0] below. Use
            # that same event-owned PR number here and fail closed when it is
            # absent; the sourceBranch validation in step 4 ensures the ADO
            # build belongs to this exact PR before any analysis can run.
            PR_NUMBER="${CHECK_PR_NUMBER}"
            HEAD_SHA="${CHECK_HEAD_SHA}"
          fi
          [ -z "${PR_NUMBER}" ] && { echo "::warning::Could not resolve a PR number."; emit_none; }
          # PR_NUMBER feeds `gh api .../pulls/<n>` and the `refs/pull/<n>/merge`
          # comparison; require it numeric so a malformed value can't reach the
          # GitHub API path (traversal-like input) or skew the branch match.
          if ! printf '%s' "${PR_NUMBER}" | grep -qE '^[0-9]+$'; then
            echo "::warning::Resolved PR number is not numeric; refusing."; emit_none
          fi

          # --- 3. Scope check: only analyse PRs targeting main / release/* ---
          PR_JSON=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          BASE_REF=$(printf '%s' "${PR_JSON}" | jq -r '.base.ref // empty')
          # An empty BASE_REF means the `gh api` call failed or returned no
          # data (rate limit / transient error), NOT that the PR targets an
          # out-of-scope branch. Treat it as a data-resolution failure so a
          # valid PR isn't silently skipped and misreported as base '' out of
          # scope.
          [ -z "${BASE_REF}" ] && { echo "::warning::Could not resolve the base ref for PR #${PR_NUMBER} (GitHub API returned no data); treating as a data-resolution failure, not an out-of-scope branch."; emit_none; }
          [ -z "${HEAD_SHA}" ] && HEAD_SHA=$(printf '%s' "${PR_JSON}" | jq -r '.head.sha // empty')
          case "${BASE_REF}" in
            main|release/*) echo "PR #${PR_NUMBER} base '${BASE_REF}' is in scope." ;;
            *) echo "::warning::PR #${PR_NUMBER} base '${BASE_REF}' is out of scope (main, release/*); skipping."; emit_none ;;
          esac

          # --- 4. Validate the build for EVERY trigger (not just dispatch):
          #        it must be the runtime definition (129), have failed, and
          #        belong to this PR (sourceBranch == refs/pull/<PR>/merge).
          #        For `check_run` the build id is parsed from a check payload
          #        we don't fully trust; for dispatch the build id and PR
          #        number are independent inputs. Validating on both paths
          #        prevents downloading an unrelated build or posting its
          #        analysis to the wrong PR.
          echo "ADO build ${BUILD_ID}: result='${RESULT}' definition='${DEF_ID}' sourceBranch='${SRC_BRANCH}'"
          if [ "${DEF_ID}" != "${ADO_BUILD_DEFINITION_ID}" ]; then
            echo "::warning::ADO build ${BUILD_ID} is definition '${DEF_ID}', not runtime (${ADO_BUILD_DEFINITION_ID}); refusing."; emit_none
          fi
          if [ "${RESULT}" != "failed" ]; then
            echo "::warning::ADO build ${BUILD_ID} did not fail (result='${RESULT}'); nothing to analyze."; emit_none
          fi
          if [ "${SRC_BRANCH}" != "refs/pull/${PR_NUMBER}/merge" ]; then
            echo "::warning::ADO build ${BUILD_ID} sourceBranch '${SRC_BRANCH}' does not match PR #${PR_NUMBER} (refs/pull/${PR_NUMBER}/merge); refusing to avoid posting to the wrong PR."; emit_none
          fi

          # Require the build's analyzed revision to equal the PR's CURRENT
          # head. Safe-output review comments are pinned to this commit, but a
          # stale analysis would still describe the wrong revision. If the PR
          # has advanced since this build ran, skip: a newer build/check for
          # the current head will cover it.
          BUILD_PR_SHA=$(printf '%s' "${build_json}" | jq -r '.triggerInfo["pr.sourceSha"] // empty')
          CURRENT_HEAD=$(printf '%s' "${PR_JSON}" | jq -r '.head.sha // empty')
          # ADO builds GitHub's `refs/pull/<n>/merge` ref, so build_json.sourceVersion
          # is the merge commit GitHub produced at build time and equals the PR's
          # `merge_commit_sha` then. If the base branch advances (even with the PR
          # head unchanged) GitHub recomputes that merge and merge_commit_sha
          # changes, so this catches base-advance staleness the head check misses.
          BUILD_MERGE_SHA=$(printf '%s' "${build_json}" | jq -r '.sourceVersion // empty')
          CURRENT_MERGE=$(printf '%s' "${PR_JSON}" | jq -r '.merge_commit_sha // empty')
          # Fail CLOSED unless both head and merge revisions are known. The
          # merge revision detects a moved base even when the head is stable.
          if [ -z "${BUILD_PR_SHA}" ] || [ -z "${CURRENT_HEAD}" ] || [ -z "${BUILD_MERGE_SHA}" ] || [ -z "${CURRENT_MERGE}" ]; then
            echo "::warning::Could not resolve all build/current head and merge revisions; skipping to avoid analyzing stale evidence."
            emit_none
          fi
          if [ "${BUILD_PR_SHA}" != "${CURRENT_HEAD}" ]; then
            echo "::warning::Build ${BUILD_ID} analyzed revision '${BUILD_PR_SHA}' but PR #${PR_NUMBER} head is now '${CURRENT_HEAD}'; skipping stale build (a newer build/check will cover the current revision)."
            emit_none
          fi
          # A difference means the base branch moved since the build.
          if [ "${BUILD_MERGE_SHA}" != "${CURRENT_MERGE}" ]; then
            echo "::warning::Build ${BUILD_ID} merge revision '${BUILD_MERGE_SHA}' but PR #${PR_NUMBER} current merge is '${CURRENT_MERGE}' (base branch advanced); skipping stale merge."
            emit_none
          fi
          # Consistent now: build revision == current PR head. Use it for
          # permalinks so they line up with the inline comments' diff target.
          HEAD_SHA="${CURRENT_HEAD}"
          echo "Analyzing build ${BUILD_ID} at PR head revision '${HEAD_SHA}'."

          # --- 5. Download failed-job Logs_Build_* artifacts and binlogs ----
          # Runtime publishes roughly 150 Logs_Build_* artifacts per PR build.
          # Use the timeline to select only failed/canceled jobs; downloading
          # every successful leg would exceed this advisory workflow's time and
          # disk budgets without adding evidence about the failing job.
          ado_get "build timeline" \
            "${ADO_API}/build/builds/${BUILD_ID}/timeline?api-version=7.1" || emit_none
          timeline_json="${ADO_DOC}"
          mapfile -t failed_job_keys < <(
            printf '%s' "${timeline_json}" |
              jq -r '.records // [] | map(select(.type == "Job" and (.result == "failed" or .result == "canceled"))) | .[].name' |
              while IFS= read -r job_name; do
                printf '%s' "${job_name}" | tr '[:upper:]' '[:lower:]' | tr -cd '[:alnum:]'
                printf '\n'
              done |
              awk 'NF && !seen[$0]++'
          )
          [ "${#failed_job_keys[@]}" -eq 0 ] && { echo "::warning::No failed or canceled jobs found in the timeline for build ${BUILD_ID}."; emit_none; }
          mapfile -t all_job_keys < <(
            printf '%s' "${timeline_json}" |
              jq -r '.records // [] | map(select(.type == "Job")) | .[].name' |
              while IFS= read -r job_name; do
                printf '%s' "${job_name}" | tr '[:upper:]' '[:lower:]' | tr -cd '[:alnum:]'
                printf '\n'
              done |
              awk 'NF && !seen[$0]++'
          )

          ado_get "artifact list" "${ADO_API}/build/builds/${BUILD_ID}/artifacts?api-version=7.1" || emit_none
          artifacts_json="${ADO_DOC}"
          mapfile -t all_names < <(printf '%s' "${artifacts_json}" | jq -r '.value // [] | map(select(.name | test("^Logs_Build_"))) | .[].name')
          mapfile -t names < <(
            for name in "${all_names[@]}"; do
              # Runtime artifact names usually equal the timeline job name,
              # but some matrices append a display-only mode such as
              # `monointerpreter`, `minijit`, or `llvmaot` to the job. Match
              # exact spellings first. Only fall back to an artifact-key
              # prefix when it identifies exactly one job in the entire
              # timeline; this avoids selecting `..._NativeAOT` for the
              # distinct `..._NativeAOT_Libraries` job.
              artifact_job_name=$(printf '%s' "${name}" | sed -E 's/^Logs_Build_(Attempt[0-9]+_)?//')
              artifact_key=$(printf '%s' "${artifact_job_name}" | tr '[:upper:]' '[:lower:]' | tr -cd '[:alnum:]')
              [ -z "${artifact_key}" ] && continue
              mapped_job_key=""
              for job_key in "${all_job_keys[@]}"; do
                if [ "${artifact_key}" = "${job_key}" ]; then
                  mapped_job_key="${job_key}"
                  break
                fi
              done
              if [ -z "${mapped_job_key}" ]; then
                prefix_matches=0
                for job_key in "${all_job_keys[@]}"; do
                  if [[ "${job_key}" == "${artifact_key}"* ]]; then
                    mapped_job_key="${job_key}"
                    prefix_matches=$((prefix_matches + 1))
                  fi
                done
                [ "${prefix_matches}" -eq 1 ] || mapped_job_key=""
              fi
              for failed_job_key in "${failed_job_keys[@]}"; do
                if [ -n "${mapped_job_key}" ] && [ "${mapped_job_key}" = "${failed_job_key}" ]; then
                  printf '%s\n' "${name}"
                  break
                fi
              done
            done
          )
          if [ "${#names[@]}" -eq 0 ]; then
            echo "::warning::No Logs_Build_* artifacts mapped unambiguously to failed or canceled jobs in build ${BUILD_ID}; the agent will inspect failed compile-task logs through hlx."
          else
            echo "Selected ${#names[@]} of ${#all_names[@]} Logs_Build_* artifacts for ${#failed_job_keys[@]} failed or canceled jobs."
          fi

          # Guards for untrusted PR-produced archives: cap the compressed
          # download and the reported uncompressed size per artifact, bound
          # extraction time, AND enforce a cumulative uncompressed budget across
          # all legs so many individually-small artifacts can't collectively
          # exhaust the runner's disk.
          # A 500 MB per-artifact cap is close enough to the size of a real
          # log artifact that an ordinary build trips it, and the job then
          # silently skips exactly the leg it exists to diagnose. Only one
          # archive is on disk at a time (each is deleted before the next
          # download), so this bounds peak zip disk use, not the sum across
          # artifacts.
          MAX_ZIP_BYTES=2147483648      # 2 GB compressed per artifact
          MAX_UNZIP_BYTES=2147483648    # 2 GB uncompressed per artifact
          MAX_TOTAL_BYTES=4294967296    # 4 GB uncompressed across all artifacts
          # Raising the per-artifact cap would otherwise raise the worst-case
          # number of bytes pulled over the network by the same factor, since
          # nothing else bounds the sum across artifacts. Cap the total
          # download too, and charge it *before* each transfer (see ZIP_CAP
          # below) rather than after, so the last artifact can't start just
          # under the limit and still pull a full MAX_ZIP_BYTES.
          MAX_TOTAL_ZIP_BYTES=3221225472  # 3 GB compressed across all artifacts
          # `--max-time` is per attempt, so `--retry N` multiplies it: the whole
          # download phase, not one transfer, is what has to fit inside this job's
          # `timeout-minutes`. Give the loop a wall-clock deadline and derive every
          # transfer's budget from what is left of it, so no combination of slow
          # artifacts and retries can take the job down before the controlled no-op.
          FETCH_BUDGET=420           # 7 minutes for all artifact transfers
          MAX_ATTEMPT_SECONDS=120       # per attempt; the full set really takes ~30s
          FETCH_DEADLINE=$(( $(date +%s) + FETCH_BUDGET ))
          TOTAL_ZIP_BYTES=0
          # One private scratch file for every download. A fixed /tmp name is a
          # pre-created symlink, or a second job on the same runner, away from being
          # someone else's file.
          ZIP_TMP=$(mktemp) || { echo "::warning::Could not create a temporary file for downloads."; emit_none; }
          # A private extraction directory, for the same reason as ZIP_TMP: a fixed
          # path is another job's directory on a runner we do not have to ourselves.
          AX_DIR=$(mktemp -d) || { echo "::warning::Could not create a temporary directory for extraction."; emit_none; }
          TOTAL_BYTES=0
          mkdir -p /tmp/binlogs
          # Only binlogs extracted by this run may be analyzed. Anything left in
          # the directory by an earlier run on the same runner would otherwise be
          # uploaded and attributed to this build.
          rm -f /tmp/binlogs/*.binlog
          count=0
          staged_legs=0
          ai=0
          for name in "${names[@]}"; do
            # `name` is PR-controlled ADO artifact metadata and the
            # `^Logs_Build_` filter only anchors the prefix, so sanitize it
            # before using it in any on-disk path or workflow command (guards
            # against path traversal and command injection); keep the original
            # `name` only for the artifacts_json lookup.
            safe_name=$(printf '%s' "${name}" | tr -c 'A-Za-z0-9._-' '_')
            ai=$((ai + 1))
            url=$(printf '%s' "${artifacts_json}" | jq -r --arg n "${name}" '.value[] | select(.name==$n) | .resource.downloadUrl // empty')
            [ -z "${url}" ] && continue
            find "${AX_DIR:?}" -mindepth 1 -delete
            : > "${ZIP_TMP}"
            # Bound this transfer by whatever is left of the cumulative budget
            # as well as by the per-artifact cap, so the two limits together
            # are a real ceiling on bytes pulled rather than
            # `MAX_TOTAL_ZIP_BYTES + MAX_ZIP_BYTES`.
            ZIP_CAP="${MAX_ZIP_BYTES}"
            ZIP_ALLOWANCE=$((MAX_TOTAL_ZIP_BYTES - TOTAL_ZIP_BYTES))
            [ "${ZIP_ALLOWANCE}" -lt "${ZIP_CAP}" ] && ZIP_CAP="${ZIP_ALLOWANCE}"
            if [ "${ZIP_CAP}" -le 0 ]; then
              echo "::warning::Cumulative compressed download budget ${MAX_TOTAL_ZIP_BYTES} is exhausted before ${safe_name}; stopping downloads."
              break
            fi
            # Bound this transfer by the time left as well, and never start one with
            # no time to finish in.
            TIME_LEFT=$(( FETCH_DEADLINE - $(date +%s) ))
            if [ "${TIME_LEFT}" -le 0 ]; then
              echo "::warning::Download time budget ${FETCH_BUDGET}s exhausted before ${safe_name}; stopping downloads."
              break
            fi
            ATTEMPT_SECONDS="${MAX_ATTEMPT_SECONDS}"
            [ "${TIME_LEFT}" -lt "${ATTEMPT_SECONDS}" ] && ATTEMPT_SECONDS="${TIME_LEFT}"
            # Download to a file, never a pipe: curl retries transient
            # 5xx/429/timeouts but can only rewind seekable output, so through
            # a pipe the retried body is APPENDED — a 503 error page followed
            # by a retry yields a corrupt `<error page><zip>` that still exits
            # 0. `--fail` keeps error bodies off disk.
            # `ulimit -f` is only a disk backstop for a response that declares
            # no Content-Length; the `-ge ZIP_CAP` guard below is
            # authoritative. Round the block count UP so any positive ZIP_CAP
            # yields at least one block: dividing down would give a 0-block
            # limit for a sub-KiB remainder and fail every write.
            # SIGXFSZ is ignored so hitting the cap is an ordinary write error
            # (23) rather than a "File size limit exceeded (core dumped)" log.
            # `--retry-max-time` only gates whether curl may *start* another retry, so a
            # retry begun just inside it can still run a further `--max-time`. `timeout`
            # around the whole invocation is what makes the deadline real rather than a
            # scheduling hint; a killed transfer is treated like any other failed one and
            # the leg is reported as missing, which fails closed.
            (
              # Fail the leg rather than the backstop: if the shell will not apply
              # the limit, downloading anyway would leave a response with no usable
              # Content-Length free to fill the disk before the size check below runs.
              # bash counts `ulimit -f` in 1024-byte units, except in POSIX mode where
              # it counts 512-byte blocks. Pin the mode so this arithmetic means one
              # thing regardless of how the runner's shell was invoked.
              set +o posix
              ulimit -f $(( (ZIP_CAP + 1023) / 1024 )) || exit 1
              trap '' XFSZ
              timeout "${TIME_LEFT}" curl -sSL --fail --retry 3 --retry-delay 2 --connect-timeout 15 --max-time "${ATTEMPT_SECONDS}" --retry-max-time "${TIME_LEFT}" -o "${ZIP_TMP}" "${url}"
            ) 2>/dev/null
            curl_rc=$?
            ZIP_BYTES=$(stat -c%s "${ZIP_TMP}" 2>/dev/null || echo 0)
            # Charge the budget with the bytes retained on disk, including those of an
            # artifact about to be skipped. This is a disk and extraction budget, not a
            # meter of network egress: `-o` truncates before each retry, so failed
            # attempts are not counted here. What bounds those is FETCH_DEADLINE via
            # the `timeout` wrapper, plus `ulimit -f`, which caps every individual
            # attempt at ZIP_CAP.
            TOTAL_ZIP_BYTES=$((TOTAL_ZIP_BYTES + ZIP_BYTES))
            if [ "${ZIP_BYTES}" -eq 0 ]; then
              echo "::warning::Skipping ${safe_name}: empty or failed download."; continue
            fi
            if [ "${ZIP_BYTES}" -ge "${ZIP_CAP}" ]; then
              echo "::warning::Skipping ${safe_name}: download reached the ${ZIP_CAP}-byte cap."; continue
            fi
            # After the size guards: hitting the ulimit cap is reported as an
            # oversized artifact above, not as a generic transfer failure.
            if [ "${curl_rc}" -ne 0 ]; then
              echo "::warning::Skipping ${safe_name}: download failed or was truncated (curl exit ${curl_rc})."; continue
            fi
            # `unzip -Zt` prints ONE summary line ("<n> files, <x> bytes
            # uncompressed, ..."), so the total comes from a fixed column
            # instead of the shifting last row of `unzip -l`. Use `END{}`:
            # Info-ZIP prepends warnings on STDOUT for a recoverable archive,
            # and a multi-line value would still pass the `grep -qE` check
            # below, since `grep -q` matches if ANY line matches. `timeout`
            # bounds a hostile archive; pipefail + fail-closed because a killed
            # probe's partial output can end in a numeric column and undercount.
            UNCOMP=$(set -o pipefail; timeout 60 unzip -Zt "${ZIP_TMP}" 2>/dev/null | awk 'END{print $3}') \
              || { echo "::warning::Skipping ${safe_name}: 'unzip -Zt' failed or timed out; cannot verify uncompressed size."; continue; }
            # Fail safe: a non-numeric size (corrupt zip, unexpected or
            # timed-out output) can't be verified, so skip rather than let it
            # bypass the guards below.
            if ! printf '%s' "${UNCOMP}" | grep -qE '^[0-9]+$'; then
              echo "::warning::Skipping ${safe_name}: could not determine uncompressed size (unparseable/timed-out unzip output)."; continue
            fi
            # ZIP64 sizes can reach ~20 digits, overflowing Bash's signed
            # 64-bit `-gt` (and the `$((...))` below), which under `set +e`
            # would let an oversized archive through. More digits than the
            # limit is unambiguously larger, so reject on length first.
            if [ "${#UNCOMP}" -gt "${#MAX_UNZIP_BYTES}" ]; then
              echo "::warning::Skipping ${safe_name}: uncompressed size has ${#UNCOMP} digits, exceeding the ${MAX_UNZIP_BYTES} guard (possible zip bomb)."; continue
            fi
            if [ "${UNCOMP}" -gt "${MAX_UNZIP_BYTES}" ]; then
              echo "::warning::Skipping ${safe_name}: uncompressed size ${UNCOMP} exceeds ${MAX_UNZIP_BYTES} guard (possible zip bomb)."; continue
            fi
            if [ $((TOTAL_BYTES + UNCOMP)) -gt "${MAX_TOTAL_BYTES}" ]; then
              echo "::warning::Cumulative uncompressed budget ${MAX_TOTAL_BYTES} reached at ${safe_name}; stopping extraction."; break
            fi
            # Refuse the archive if any entry path is absolute or has a `..`
            # component (defense-in-depth over unzip's own traversal guard),
            # then extract `*.binlog` entries *preserving* their in-archive
            # paths (no `-j`) under a fresh dir + timeout, so two binlogs that
            # share a basename in different folders don't overwrite each other.
            # The listing is streamed through `grep` (no full in-memory buffer
            # of entry names) and PIPESTATUS separates the failure modes: a
            # non-zero listing exit (error/timeout) FAILS CLOSED; a grep match
            # means a suspicious absolute/`..` path.
            timeout 60 unzip -Z1 "${ZIP_TMP}" 2>/dev/null | grep -qE '(^/|(^|/)\.\.(/|$))'
            zscan_rc=("${PIPESTATUS[@]}")
            # Check the match first: grep -q can close the pipe early and make
            # unzip report SIGPIPE for the same suspicious-path result.
            if [ "${zscan_rc[1]}" -eq 0 ]; then
              echo "::warning::Skipping ${safe_name}: archive has a suspicious (absolute or ..) entry path."; continue
            fi
            if [ "${zscan_rc[0]}" -ne 0 ]; then
              echo "::warning::Skipping ${safe_name}: could not list archive entries (unzip -Z1 rc=${zscan_rc[0]})."; continue
            fi
            # Extraction shares the deadline with the transfers. Otherwise a run that
            # spent most of its budget downloading could still queue one bounded
            # extraction per artifact and walk the job past `timeout-minutes` without
            # ever reaching the controlled no-op below.
            TIME_LEFT=$(( FETCH_DEADLINE - $(date +%s) ))
            if [ "${TIME_LEFT}" -le 0 ]; then
              echo "::warning::Fetch budget exhausted before extracting ${safe_name}; stopping."; break
            fi
            [ "${TIME_LEFT}" -gt 120 ] && TIME_LEFT=120
            timeout "${TIME_LEFT}" unzip -o "${ZIP_TMP}" '*.binlog' -d "${AX_DIR}" >/dev/null 2>&1 \
              || { echo "::warning::Skipping ${safe_name}: extraction failed or timed out."; continue; }
            # Consume the budget only once the archive actually extracted, so a
            # skipped leg can't exhaust it and force later legs to be dropped.
            TOTAL_BYTES=$((TOTAL_BYTES + UNCOMP))
            i=0
            leg_staged=0
            leg_failed=0
            count_before_leg="${count}"
            while IFS= read -r bl; do
              [ -f "${bl}" ] || continue
              # Prefixing with the artifact index (`ai`) and per-file counter
              # (`i`) keeps destinations unique, so neither a cross-artifact
              # sanitize collision nor same-basename entries can overwrite a
              # staged binlog. `safe_name` is kept only for readability.
              dest="/tmp/binlogs/${ai}_${i}_${safe_name}.binlog"
              # Advance before copying so a failed copy can never cause the
              # next entry to reuse a possibly partially-created destination.
              i=$((i + 1))
              # Count only a successful copy — `set +e` is on, so a failed `cp`
              # must not inflate the counts.
              if cp "${bl}" "${dest}"; then
                count=$((count + 1))
                leg_staged=$((leg_staged + 1))
              else
                leg_failed=1
                # `bl` is an entry name from a PR-produced archive, so it can
                # carry newlines or `::` and forge workflow commands. Report
                # the destination, which is built only from the artifact index,
                # the per-file counter and the sanitized artifact name.
                echo "::warning::Failed to stage an entry of ${safe_name} as ${dest}; skipping."
              fi
            done < <(find "${AX_DIR}" -type f -name '*.binlog')
            # Keep each artifact all-or-nothing. A partial leg can hide the
            # actual root cause, so discard its staged files and let hlx cover
            # the failed task logs instead.
            if [ "${leg_failed}" -ne 0 ]; then
              find /tmp/binlogs -maxdepth 1 -type f -name "${ai}_*_${safe_name}.binlog" -delete
              count="${count_before_leg}"
              echo "::warning::Skipping ${safe_name}: not every extracted binlog could be staged."
              continue
            fi
            [ "${leg_staged}" -gt 0 ] && staged_legs=$((staged_legs + 1))
          done
          rm -rf "${AX_DIR:?}" "${ZIP_TMP}"
          echo "Extracted ${count} binlog(s) from ${staged_legs}/${#names[@]} selected artifacts into /tmp/binlogs:"
          ls -la /tmp/binlogs || true
          binlog_found=false
          [ "${count}" -gt 0 ] && binlog_found=true
          [ "${count}" -eq 0 ] && echo "::warning::No usable *.binlog was staged; the agent will inspect failed compile-task logs through hlx."
          # A missing archive is no longer an analysis blind spot: the agent
          # must inspect every failed compile task through hlx and use binlogs
          # as higher-fidelity evidence where available.
          if [ "${staged_legs}" -ne "${#names[@]}" ]; then
            echo "::warning::Only ${staged_legs} of ${#names[@]} selected Logs_Build_* artifacts produced usable binlogs; hlx will cover the missing failed-task logs."
          fi

          # The download/extract loop above can take minutes. Re-read the PR
          # head right before activating and fail CLOSED if it moved or can't
          # be resolved: a force-push during that window would otherwise leave
          # the analyzed evidence stale relative to the current PR revision.
          LATEST_PR=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          LATEST_HEAD=$(printf '%s' "${LATEST_PR}" | jq -r '.head.sha // empty')
          LATEST_MERGE=$(printf '%s' "${LATEST_PR}" | jq -r '.merge_commit_sha // empty')
          if [ -z "${LATEST_HEAD}" ] || [ "${LATEST_HEAD}" != "${HEAD_SHA}" ]; then
            echo "::warning::PR #${PR_NUMBER} head changed during artifact download ('${HEAD_SHA}' -> '${LATEST_HEAD}') or could not be re-resolved; skipping to avoid posting stale-build suggestions against the new diff."
            emit_none
          fi
          if [ -z "${LATEST_MERGE}" ]; then
            echo "::warning::Could not re-resolve PR #${PR_NUMBER}'s merge revision after artifact download; skipping."
            emit_none
          fi
          # The base branch may also have advanced during the download.
          if [ "${LATEST_MERGE}" != "${BUILD_MERGE_SHA}" ]; then
            echo "::warning::PR #${PR_NUMBER} merge revision changed during artifact download ('${BUILD_MERGE_SHA}' -> '${LATEST_MERGE}'); skipping stale merge."
            emit_none
          fi

          {
            echo "analysis-ready=true"
            echo "binlog-found=${binlog_found}"
            echo "pr-number=${PR_NUMBER}"
            echo "pr-head-sha=${HEAD_SHA}"
            echo "pr-merge-sha=${BUILD_MERGE_SHA}"
            echo "ado-build-id=${BUILD_ID}"
            echo "ado-build-url=${ADO_BUILD_UI}?buildId=${BUILD_ID}"
          } >> "$GITHUB_OUTPUT"

      - name: Upload analysis artifact
        if: steps.fetch.outputs.binlog-found == 'true'
        uses: actions/upload-artifact@v7.0.1
        with:
          name: build-failure-analysis-data
          path: /tmp/binlogs
          if-no-files-found: warn
          retention-days: 1

# Steps that run in the agent job after the failed build and target revision
# are verified. Binlog download is conditional; when no matching binlog was
# published, the agent analyzes failed compile-task logs through hlx.
steps:
  - name: Download analysis artifact
    if: needs.fetch-binlog.outputs.binlog-found == 'true'
    uses: actions/download-artifact@v8.0.1
    with:
      name: build-failure-analysis-data
      path: /tmp/binlogs

  - name: Export agent context
    shell: bash
    env:
      GH_AW_BINLOG_FOUND_VALUE: ${{ needs.fetch-binlog.outputs.binlog-found }}
      GH_AW_PR_NUMBER_VALUE: ${{ needs.fetch-binlog.outputs.pr-number }}
      GH_AW_PR_HEAD_SHA_VALUE: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
      GH_AW_PR_MERGE_SHA_VALUE: ${{ needs.fetch-binlog.outputs.pr-merge-sha }}
      GH_AW_ADO_BUILD_URL_VALUE: ${{ needs.fetch-binlog.outputs.ado-build-url }}
      GH_AW_GITHUB_WORKSPACE: ${{ github.workspace }}
    run: |
      # The binlogs are mounted into the binlog-mcp container at
      # `/data/binlogs`. Build the list of in-container binlog paths (one per
      # selected artifact) that the agent should query. `GH_AW_BINLOG_PATH` is
      # the first entry for tools/prompts that expect a single path.
      BINLOG_DIR="/data/binlogs"
      LIST=""
      if [ "${GH_AW_BINLOG_FOUND_VALUE:-false}" = "true" ] && [ -d /tmp/binlogs ]; then
        for f in /tmp/binlogs/*.binlog; do
          [ -f "$f" ] || continue
          LIST="${LIST}${BINLOG_DIR}/$(basename "$f")"$'\n'
        done
      fi
      # `shell: bash` puts this step under `-eo pipefail`, so take the first
      # entry with a parameter expansion instead of `printf | head -1`: a pipe
      # whose reader exits early would raise SIGPIPE and abort the step.
      FIRST=${LIST%%$'\n'*}
      {
        echo "GH_AW_BUILD_OUTCOME=failure"
        echo "GH_AW_BINLOG_DIR=${BINLOG_DIR}"
        echo "GH_AW_BINLOG_PATH=${FIRST}"
        echo "GH_AW_BINLOG_HOST_PATH=${GH_AW_ADO_BUILD_URL_VALUE}"
        echo "GH_AW_PR_NUMBER=${GH_AW_PR_NUMBER_VALUE}"
        echo "GH_AW_PR_HEAD_SHA=${GH_AW_PR_HEAD_SHA_VALUE}"
        echo "GH_AW_PR_MERGE_SHA=${GH_AW_PR_MERGE_SHA_VALUE}"
        echo "GH_AW_WORKSPACE=${GH_AW_GITHUB_WORKSPACE}"
        echo "GH_AW_BINLOG_LIST<<GH_AW_EOF"
        printf '%s' "$LIST"
        echo "GH_AW_EOF"
      } >> "$GITHUB_ENV"

tools:
  github:
    toolsets: [pull_requests, repos]
  bash:
    - "cat"
    - "head"
    - "tail"
    - "grep"
    - "wc"
    - "sort"
    - "uniq"
    - "ls"
    - "find"
    # binlog-mcp is also mounted as a CLI wrapper (…/mcp-cli/bin/binlog-mcp);
    # allow it so the agent can query the binlogs via the wrapper when it does
    # not call the MCP tool natively.
    - "binlog-mcp:*"

safe-outputs:
  needs: [fetch-binlog]
  steps:
    - name: Revalidate PR revision before applying queued outputs
      shell: bash
      env:
        GH_TOKEN: ${{ github.token }}
        GH_AW_REPO: ${{ github.repository }}
        PR_NUMBER: ${{ needs.fetch-binlog.outputs.pr-number }}
        EXPECTED_HEAD: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
        EXPECTED_MERGE: ${{ needs.fetch-binlog.outputs.pr-merge-sha }}
      run: |
        set -euo pipefail
        if [ -z "${EXPECTED_HEAD}" ] || [ -z "${EXPECTED_MERGE}" ] ||
           ! gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" |
             jq -e --arg head "${EXPECTED_HEAD}" --arg merge "${EXPECTED_MERGE}" \
               '.head.sha == $head and .merge_commit_sha == $merge' >/dev/null; then
          echo "::error::PR #${PR_NUMBER} moved or could not be verified before applying queued build-analysis outputs."
          exit 1
        fi
  messages:
    footer: "> 🤖 **Automated content by GitHub Copilot.** Generated by the [{workflow_name}]({agentic_workflow_url}) workflow.{ai_credits_suffix} · [◷]({history_link})"
  data:
    type: object
    properties:
      workflow_artifact:
        type: string
        enum: [build-failure-analysis]
      artifact_kind:
        type: string
        enum: [analysis]
    required: [workflow_artifact, artifact_kind]
    additionalProperties: false
  # Bind writes to the PR number in the trusted trigger rather than allowing
  # untrusted binlog/source content to choose an arbitrary repository target.
  # The fetch job uses the same value and verifies that the ADO build's
  # sourceBranch belongs to it before the agent can run.
  report-failure-as-issue: false
  add-comment:
    max: 1
    target: ${{ github.event.check_run.pull_requests[0].number || inputs['pr-number'] }}
    hide-older-comments: true
  create-pull-request-review-comment:
    max: 25
    target: ${{ github.event.check_run.pull_requests[0].number || inputs['pr-number'] }}
    commit-id: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
  noop:
    max: 1
    report-as-issue: false
---

<!--
  Body provided by shared/build-failure-analysis-shared.md.

  All build-failure analysis expertise (binlog parsing, error grouping,
  suggestion authoring) lives in the reusable agent at
  .github/agents/build-failure-analyst.agent.md.
-->
