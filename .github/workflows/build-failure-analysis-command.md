---
name: "Build Failure Analysis (command)"
description: >-
  Rerun the build-failure analysis on a pull request when a maintainer comments
  `/analyze-build-failure`. Same body as `build-failure-analysis.md` — it does
  NOT rebuild: it inspects the PR's **latest** Azure Pipelines `runtime`
  build and, **only when that latest build has failed** (it stops if the
  newest build is still running or has succeeded), reuses available binary
  logs and falls back to failed Azure DevOps compile-task logs when Runtime
  did not publish a matching binlog artifact.

on:
  slash_command:
    name: analyze-build-failure
    events: [pull_request_comment]
  roles: [admin, maintainer, write]
  reaction: "eyes"
  # Gate the AI pipeline on the fetch job so the agent only runs after the
  # failed Azure DevOps build and target revision have been verified.
  needs: [fetch-binlog]

# Runtime compile jobs do not always publish binlogs. Once the failed build is
# verified, analyze available binlogs and use the bounded Azure DevOps log
# fallback for failed compile tasks without them.
if: needs.fetch-binlog.outputs.analysis-ready == 'true'

# Least-privilege for the workflow/agent jobs. The agent runs read-only; it
# does NOT post directly. All PR writes it produces (summary comment + inline
# review suggestions) go through gh-aw **safe-outputs**, which the compiler
# emits as a separate `safe_outputs` job granted `pull-requests: write` +
# `issues: write` in the generated lock. (The slash-command trigger also adds
# an acknowledgement reaction to the command comment; gh-aw emits that in its
# own generated job with the scope it needs — it is not driven by this agent
# job.) Keep `pull-requests: read` here so the AI agent job stays
# least-privilege — do NOT raise it to `write`, that would hand PR-write scope
# to the agent job unnecessarily.
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
  # Distinct from the automatic workflow's group (`build-failure-analysis-<pr>`).
  # Concurrency groups are repository-global, so sharing the name made the two
  # workflows cancel each other for the same PR: a newly failing build would
  # kill an on-demand analysis a maintainer had just asked for. Each still
  # collapses its own repeat invocations for a PR.
  group: build-failure-analysis-cmd-${{ github.event.issue.number || github.event.pull_request.number || fromJSON(github.event.inputs.aw_context || github.event.client_payload.aw_context || '{}').item_number || github.run_id }}
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

# Live binlog access for the agent — see build-failure-analysis.md for the
# rationale. The fetch-binlog job downloads failed-job binlogs from Azure
# DevOps into a directory and uploads them; the agent job downloads them to
# `/tmp/binlogs` and the gh-aw MCP gateway mounts it read-only at
# `/data/binlogs`.
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

# Custom job that reuses the binlogs from the PR's most recent failed Azure
# DevOps `runtime` build instead of rebuilding. Mirrors the fetch-binlog job
# in build-failure-analysis.md; it locates the build by the PR's merge branch
# (no `check_run` payload is available on a slash command).
jobs:
  fetch-binlog:
    name: Fetch binlogs (Azure Pipelines)
    # Cheap pre-gate. This job is a dependency of gh-aw's `pre_activation`, so it
    # runs BEFORE the role / command-position check. Without a guard it would
    # download hundreds of MB of binlogs on *every* comment in the repository,
    # which any public commenter could trigger repeatedly. This expression is
    # only the free first filter — `author_association` is coarse (in an
    # org-owned repo every org member reports MEMBER regardless of the
    # permission they actually hold here), so the step below resolves the
    # commenter's real repository permission before anything is downloaded.
    # `pre_activation` remains the authoritative role + command-position check,
    # and `activation` additionally requires `binlog-found == 'true'`.
    #
    # KEEP IN SYNC with `roles:` in the frontmatter above. The author_association
    # list here and the permission step below are hand-written restatements of
    # that policy; editing `roles:` does NOT update them, because only
    # `pre_activation` is generated from the frontmatter.
    #
    # `github.event.issue.pull_request` is what keeps plain issue comments out:
    # gh-aw emits no such filter of its own despite `events: [pull_request_comment]`
    # (checked in the generated lock), so PR-only scoping is a property of this
    # hand-written expression rather than something the compiler enforces. It
    # degrades safely without it — `repos/.../pulls/<issue#>` 404s and the script
    # emits no binlog — but it would pay for a runner first.
    #
    # `contains(..., '/analyze-build-failure')` is a substring match anywhere in
    # the body, whereas the authoritative `check_command_position` requires the
    # command to be in a valid position. So a write-access user merely mentioning
    # the command, or editing an old comment that quotes it (`types:` includes
    # `edited`), still starts this job. Workflow `if:` expressions have no
    # regex, and `startsWith` would reject the leading whitespace/newlines gh-aw
    # accepts, so this stays a deliberate over-approximation — but it is now
    # only a cheap pre-filter: the first step of the job reproduces gh-aw's real
    # first-token check and bails out before anything is downloaded.
    if: >-
      github.event.repository.fork == false &&
      github.event.issue.pull_request &&
      contains(fromJSON('["OWNER","MEMBER","COLLABORATOR"]'), github.event.comment.author_association) &&
      contains(github.event.comment.body, '/analyze-build-failure')
    runs-on: ubuntu-latest
    timeout-minutes: 15
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
      # `author_association` in the job-level `if:` cannot tell an org member
      # with read-only access apart from a maintainer, so resolve the real
      # repository permission here — before any download — and match it against
      # the same `roles: [admin, maintainer, write]` this command declares.
      # KEEP IN SYNC with that list.
      #
      # `.permission` is the field to test. The REST docs for this endpoint say
      # it returns the legacy base roles admin|write|read|none, "where the
      # maintain role is mapped to write and the triage role is mapped to read",
      # so `admin|write` is exactly "has push access or better" — precisely the
      # set `roles: [admin, maintainer, write]` describes, with maintainers
      # included.
      #
      # `.role_name` is deliberately NOT consulted. It reports "the name of the
      # assigned role, including custom roles", and a custom organization role
      # only has to avoid the base names read/triage/write/maintain/admin — so
      # matching on it would let a role merely *named* like a privileged one
      # (e.g. a custom `maintainer` inheriting read) pass this gate with no push
      # access at all.
      #
      # On any API failure the response carries no `.permission`, so `perm` ends
      # up empty and the check falls into the deny branch; failing closed is the
      # safe direction for a pre-gate.
      - name: Verify the comment invokes the command and the commenter has write access
        id: perm
        if: github.event_name == 'issue_comment'
        shell: bash
        env:
          GH_TOKEN: ${{ github.token }}
          COMMENTER: ${{ github.event.comment.user.login }}
          COMMENT_BODY: ${{ github.event.comment.body }}
          COMMAND_NAME: "analyze-build-failure"
        run: |
          set +e
          # --- 1. Command position (free; do this before the API call) ------
          # The job-level `if:` can only use `contains()`, a plain substring
          # test, so a comment that merely mentions the command — or an edited
          # old comment quoting it — still reaches this job and pays for the
          # download before `pre_activation` throws the result away. That check
          # runs too late by construction, so reproduce it here.
          #
          # gh-aw trims the body and requires the command to be the FIRST token:
          # `/^\/([a-zA-Z0-9][a-zA-Z0-9._-]*)(?=$|\s)/` over the trimmed text,
          # then an equality comparison on the captured name
          # (actions/setup/js/slash_command_matcher.cjs). `awk 'NF {print $1;
          # exit}'` is the same rule: skip leading whitespace/blank lines, take
          # the first whitespace-delimited token. The token is delimited by
          # whitespace or end-of-input, exactly the `(?=$|\s)` lookahead, so
          # `/analyze-build-failure-now` correctly does NOT match. `tr -d '\r'`
          # is needed because JS `.trim()` and `\s` treat CR as whitespace while
          # awk's default field splitting does not.
          # KEEP IN SYNC with `on.command.name` below.
          first_word=$(printf '%s' "${COMMENT_BODY}" | tr -d '\r' | awk 'NF {print $1; exit}')
          if [ "${first_word}" != "/${COMMAND_NAME}" ]; then
            # Never echo the raw token: it is attacker-controlled and `::`-
            # prefixed text is interpreted by the runner as a workflow command.
            safe_word=$(printf '%s' "${first_word}" | tr -cd 'A-Za-z0-9/._-' | cut -c1-40)
            echo "Comment does not start with '/${COMMAND_NAME}' (first token: '${safe_word}'); skipping the binlog download."
            echo "authorized=false" >> "$GITHUB_OUTPUT"
            exit 0
          fi
          # --- 2. Repository permission -------------------------------------
          # `COMMENTER` is interpolated into an API path and into log output, so
          # give it the same shape check `PR_NUMBER` and `BUILD_ID` get below.
          # GitHub logins are alphanumerics and hyphens; anything else (a bot
          # login such as `github-actions[bot]`, or an empty value) is rejected
          # here instead of being sent to the API.
          if ! printf '%s' "${COMMENTER}" | grep -qE '^[A-Za-z0-9-]+$'; then
            echo "::warning::Commenter login is missing or malformed; skipping the binlog download."
            echo "authorized=false" >> "$GITHUB_OUTPUT"
            exit 0
          fi
          # Read the response first and extract with `jq` rather than using
          # `gh api --jq`: on a non-2xx response `gh` prints the error document
          # to stdout, which `--jq` does not filter, so the raw JSON would end
          # up in `perm` and get echoed into the log. Extracting the field
          # ourselves yields an empty string for any error shape.
          resp=$(gh api "repos/${GITHUB_REPOSITORY}/collaborators/${COMMENTER}/permission" 2>/dev/null)
          perm=$(printf '%s' "${resp}" | jq -r '.permission // empty' 2>/dev/null)
          case "${perm}" in
            admin|write) authorized=true ;;
            *)           authorized=false ;;
          esac
          if [ "${authorized}" = "true" ]; then
            echo "'${COMMENTER}' has '${perm}' access to ${GITHUB_REPOSITORY}; proceeding."
          else
            echo "::warning::'${COMMENTER}' does not have write access to ${GITHUB_REPOSITORY} (resolved permission '${perm:-none}'); skipping the binlog download."
          fi
          echo "authorized=${authorized}" >> "$GITHUB_OUTPUT"

      - name: Download binlogs from the PR's latest failed Azure Pipelines build
        id: fetch
        if: github.event_name != 'issue_comment' || steps.perm.outputs.authorized == 'true'
        shell: bash
        env:
          GH_TOKEN: ${{ github.token }}
          GH_AW_REPO: ${{ github.repository }}
          ADO_API: "https://dev.azure.com/dnceng-public/public/_apis"
          ADO_BUILD_UI: "https://dev.azure.com/dnceng-public/public/_build/results"
          # runtime pipeline definition id in dnceng-public/public.
          ADO_BUILD_DEFINITION_ID: "129"
          PR_NUMBER: ${{ github.event.issue.number || fromJSON(github.event.inputs.aw_context || github.event.client_payload.aw_context || '{}').item_number }}
        run: |
          # Advisory + fail-closed. On any validation gap keep the agent inert.
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

          [ -z "${PR_NUMBER}" ] && { echo "::warning::No PR number resolved from the slash-command event / aw_context."; emit_none; }
          # PR_NUMBER feeds GitHub API paths and the `refs/pull/<n>/merge`
          # branch query; require it numeric so a malformed event/aw_context
          # payload can't reach those URLs with unexpected content.
          if ! printf '%s' "${PR_NUMBER}" | grep -qE '^[0-9]+$'; then
            echo "::warning::Resolved PR number is not numeric; refusing."; emit_none
          fi

          # --- Scope check: only analyse PRs targeting main / release/* ---
          PR_JSON=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          BASE_REF=$(printf '%s' "${PR_JSON}" | jq -r '.base.ref // empty')
          # An empty BASE_REF means the `gh api` call failed or returned no
          # data (rate limit / transient error), NOT that the PR targets an
          # out-of-scope branch. Treat it as a data-resolution failure so a
          # valid PR isn't silently skipped and misreported as base '' out of
          # scope.
          [ -z "${BASE_REF}" ] && { echo "::warning::Could not resolve the base ref for PR #${PR_NUMBER} (GitHub API returned no data); treating as a data-resolution failure, not an out-of-scope branch."; emit_none; }
          HEAD_SHA=$(printf '%s' "${PR_JSON}" | jq -r '.head.sha // empty')
          case "${BASE_REF}" in
            main|release/*) echo "PR #${PR_NUMBER} base '${BASE_REF}' is in scope." ;;
            *) echo "::warning::PR #${PR_NUMBER} base '${BASE_REF}' is out of scope (main, release/*); skipping."; emit_none ;;
          esac

          # --- Find the PR's most recent runtime build (merge ref) -----------
          # Query the newest build REGARDLESS of status (queue-time desc). If
          # the newest build is still queued/running — e.g. right after a
          # force-push — skip: analysing an older completed failure now would
          # pair a stale binlog with the PR's current head. Only proceed when
          # the newest build is completed AND failed. The head SHA is then
          # anchored to that build's own revision (below), so links/suggestions
          # always match the analysed binlog.
          ado_get "build list" \
            "${ADO_API}/build/builds?definitions=${ADO_BUILD_DEFINITION_ID}&branchName=refs/pull/${PR_NUMBER}/merge&queryOrder=queueTimeDescending&\$top=1&api-version=7.1" || emit_none
          builds_json="${ADO_DOC}"
          BUILD_ID=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].id // empty')
          BUILD_STATUS=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].status // empty')
          BUILD_RESULT=$(printf '%s' "${builds_json}" | jq -r '.value // [] | .[0].result // empty')
          [ -z "${BUILD_ID}" ] && { echo "::warning::No runtime build found for PR #${PR_NUMBER}."; emit_none; }
          # Require a numeric build id before it feeds subsequent ADO API URLs,
          # so a malformed query response can't inject unexpected path/query.
          if ! printf '%s' "${BUILD_ID}" | grep -qE '^[0-9]+$'; then
            echo "::warning::ADO build id is not numeric; refusing."; emit_none
          fi
          echo "Newest runtime build for PR #${PR_NUMBER}: id='${BUILD_ID}' status='${BUILD_STATUS}' result='${BUILD_RESULT}'"
          if [ "${BUILD_STATUS}" != "completed" ]; then
            echo "::warning::PR #${PR_NUMBER}'s newest runtime build (${BUILD_ID}) is still '${BUILD_STATUS}'; wait for it to finish before analysing."
            emit_none
          fi
          if [ "${BUILD_RESULT}" != "failed" ]; then
            echo "::warning::PR #${PR_NUMBER}'s newest runtime build (${BUILD_ID}) result is '${BUILD_RESULT}', not failed — the failure looks resolved; nothing to analyse."
            emit_none
          fi

          # Require the build's analyzed revision to equal the PR's CURRENT
          # head. Safe-output review comments are pinned to this commit, but a
          # stale analysis would still describe the wrong revision. The PR can
          # advance between selecting the build and downloading artifacts, and
          # right after a force-push this query can still return the previous
          # failed build — so re-read the head here and skip if it moved.
          ado_get "build metadata" "${ADO_API}/build/builds/${BUILD_ID}?api-version=7.1" || emit_none
          build_json="${ADO_DOC}"
          BUILD_PR_SHA=$(printf '%s' "${build_json}" | jq -r '.triggerInfo["pr.sourceSha"] // empty')
          BUILD_MERGE_SHA=$(printf '%s' "${build_json}" | jq -r '.sourceVersion // empty')
          PR_JSON2=$(gh api "repos/${GH_AW_REPO}/pulls/${PR_NUMBER}" 2>/dev/null)
          CURRENT_HEAD=$(printf '%s' "${PR_JSON2}" | jq -r '.head.sha // empty')
          CURRENT_MERGE=$(printf '%s' "${PR_JSON2}" | jq -r '.merge_commit_sha // empty')
          # Fail CLOSED unless both head and merge revisions are known. The
          # merge revision detects a moved base even when the head is stable.
          if [ -z "${BUILD_PR_SHA}" ] || [ -z "${CURRENT_HEAD}" ] || [ -z "${BUILD_MERGE_SHA}" ] || [ -z "${CURRENT_MERGE}" ]; then
            echo "::warning::Could not resolve all build/current head and merge revisions; skipping."
            emit_none
          fi
          if [ "${BUILD_PR_SHA}" != "${CURRENT_HEAD}" ]; then
            echo "::warning::Build ${BUILD_ID} analyzed revision '${BUILD_PR_SHA}' but PR #${PR_NUMBER} head is now '${CURRENT_HEAD}'; skipping stale build (a newer build will cover the current revision)."
            emit_none
          fi
          # ADO builds GitHub's `refs/pull/<n>/merge` ref, so build_json.sourceVersion
          # is that merge commit; a difference means the base branch moved.
          if [ "${BUILD_MERGE_SHA}" != "${CURRENT_MERGE}" ]; then
            echo "::warning::Build ${BUILD_ID} merge revision '${BUILD_MERGE_SHA}' but PR #${PR_NUMBER} current merge is '${CURRENT_MERGE}' (base branch advanced); skipping stale merge."
            emit_none
          fi
          HEAD_SHA="${CURRENT_HEAD}"
          echo "Analyzing build ${BUILD_ID} at PR head revision '${HEAD_SHA}'."

          # --- Download failed-job Logs_Build_* artifacts and binlogs ------
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
      # See build-failure-analysis.md for the binlog path conventions. The
      # failed-job binlogs are read through the binlog-mcp MCP server (mounted
      # at `/data/binlogs`); GH_AW_BINLOG_HOST_PATH points at the Azure DevOps
      # build for human-facing references.
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
  # This workflow is triggered by an `issue_comment` on a PR, so it HAS a
  # triggering item — and it is the same PR `fetch-binlog` resolves from
  # `github.event.issue.number`. Binding to it prevents untrusted binlog/source
  # content from selecting a different repository target.
  report-failure-as-issue: false
  add-comment:
    max: 1
    target: "triggering"
    hide-older-comments: true
  create-pull-request-review-comment:
    max: 25
    target: "triggering"
    commit-id: ${{ needs.fetch-binlog.outputs.pr-head-sha }}
  noop:
    max: 1
    report-as-issue: false
---

<!--
  Body provided by shared/build-failure-analysis-shared.md.
-->
