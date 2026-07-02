---
name: "Closed Issue Reference Check"
description: "Periodic check for closed issues whose numbers still appear in the source tree. A deterministic pre-step collects the references and resolves each issue's state; the agent re-checks every closed-but-referenced issue and posts one advisory comment recommending it be reopened or the reference removed."

permissions:
  contents: read
  issues: read
  pull-requests: read

on:
  schedule: weekly
  workflow_dispatch:
  roles: [admin, maintainer, write]
  permissions: {}

if: |
  github.repository == 'dotnet/runtime'

# ###############################################################
# Override COPILOT_GITHUB_TOKEN with a random PAT from the pool.
# This stop-gap will be removed when org billing is available.
# See: .github/workflows/shared/pat_pool.README.md for more info.
# ###############################################################
imports:
  - shared/pat_pool.md

engine:
  id: copilot
  model: claude-opus-4.6
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

concurrency:
  group: "closed-issue-reference-check"
  cancel-in-progress: true

tools:
  github:
    toolsets: [issues, pull_requests, repos, search]
    min-integrity: merged
  bash: ["git", "find", "ls", "cat", "grep", "head", "tail", "wc", "jq", "tee", "sed", "awk", "tr", "cut", "sort", "uniq", "xargs", "echo", "date", "mkdir", "test", "env", "basename", "dirname", "gh"]

checkout:
  fetch-depth: 1

steps:
  - name: Collect closed issues still referenced in code (deterministic)
    env:
      SCAN_REPO: ${{ github.repository }}
      SCAN_DIRS: "src"
      SCAN_MAX: "30"
      SCAN_OUT: ${{ github.workspace }}/issue-candidates.json
      GH_TOKEN: ${{ github.token }}
    run: |
      set -euo pipefail

      # Find closed issues still referenced in *.cs, ranked by reference count, into issue-candidates.json.

      if [ -n "${NODE_EXTRA_CA_CERTS:-}" ] && [ -z "${SSL_CERT_FILE:-}" ]; then
        export SSL_CERT_FILE="$NODE_EXTRA_CA_CERTS"
      fi

      REPO="${SCAN_REPO:?}"
      DIRS="${SCAN_DIRS:-src}"
      MAX="${SCAN_MAX:-30}"
      OUT="${SCAN_OUT:-issue-candidates.json}"
      owner="${REPO%/*}"
      name="${REPO#*/}"

      refs="$(mktemp)"
      grep -rEnI "${owner}/${name}/issues/[0-9]+" $DIRS --include=*.cs 2>/dev/null \
        | awk -v pat="${owner}/${name}/issues/" '
            { split($0, a, ":"); path=a[1]; lineno=a[2];
              n=$0; sub(".*" pat, "", n); sub(/[^0-9].*/, "", n);
              if (n != "") print n "\t" path ":" lineno }' > "$refs"

      nums_file="$(mktemp)"
      cut -f1 "$refs" | sort -un > "$nums_file"
      echo "scan: $(wc -l < "$nums_file" | tr -d ' ') unique referenced issue numbers under ${DIRS}"

      states="$(mktemp)"
      emit_query() {
        local parts="$1"
        [ -z "$parts" ] && return 0
        local q="query{repository(owner:\"$owner\",name:\"$name\"){ ${parts} }}"
        gh api graphql -f query="$q" 2>/dev/null \
          | jq -c '.data.repository | to_entries[] | select(.value!=null) | .value' >> "$states" || true
      }
      parts=""; cnt=0
      while read -r n; do
        parts+="i${n}: issue(number:${n}){number state title url} "
        cnt=$((cnt+1))
        if [ "$cnt" -ge 100 ]; then emit_query "$parts"; parts=""; cnt=0; fi
      done < "$nums_file"
      emit_query "$parts"

      jq -R 'split("\t") | {number:(.[0]|tonumber), ref:.[1]}' "$refs" \
        | jq -s 'group_by(.number)
                 | map({number:.[0].number, count: length, refs: ([.[].ref] | unique)})' > "${refs}.grouped"

      jq -s --slurpfile grouped "${refs}.grouped" --argjson max "$MAX" '
        ([.[] | select(.state=="CLOSED")]) as $closed
        | ($grouped[0]) as $g
        | [ $closed[] | . as $c
            | ($g[] | select(.number==$c.number)) as $r
            | select($r != null)
            | { number:$c.number, url:$c.url, title:$c.title,
                refs:($r.refs[0:15]), _count:$r.count } ]
        | sort_by(-._count) | .[0:$max] | map(del(._count))
      ' "$states" > "$OUT"

      rm -f "$refs" "${refs}.grouped" "$nums_file" "$states"
      count="$(jq 'length' "$OUT")"
      echo "scan: ${count} closed issue(s) still referenced in code -> ${OUT}"
      jq -r '.[] | "  #\(.number) (\(.refs|length) refs) \(.title)"' "$OUT" || true
      if [ "$count" -eq 0 ] && [ -n "${GH_AW_SAFE_OUTPUTS:-}" ]; then
        echo '{"type":"noop","message":"No closed issues are still referenced in code."}' >> "$GH_AW_SAFE_OUTPUTS"
      fi

safe-outputs:
  add-comment:
    target: "*"
    max: 30
  noop:
    report-as-issue: false

timeout-minutes: 60

network:
  allowed:
    - defaults
    - github
---

# Closed Issue Reference Check

You reconcile closed issues with the code that still references them. A deterministic step has already scanned the source tree and written `issue-candidates.json` to the workspace root: every entry is a closed issue whose number still appears somewhere in the code. That usually means one of two things is out of date — the reference should be removed because the issue was genuinely resolved, or the issue was closed prematurely and should be reopened. Confirm each case and leave a single comment so a maintainer can decide.

You only suggest; you never act. The one write you can make is an `add-comment` through `safe-outputs`. Do not change issue state, edit files, or use `gh` to write anything.

## Guardrails

- **Work from `issue-candidates.json` only.** The scan already enumerated the issues and collected their code references, so re-deriving them wastes time and risks drift. Read a candidate's detail to verify it, but never grow the candidate set.
- **Read issue content through the `github` MCP tools** (`min-integrity: merged`, the strictest gate — only content authored by established maintainers is trusted). If a result comes back `[Filtered]`, skip it and note the count rather than reaching for `gh issue view` or `gh api` to get around the gate — `gh` is only for cheap metadata the MCP tools cannot express.
- **One comment per issue, ever.** Each comment carries the hidden marker `<!-- closed-issue-reference-check:advice -->`; if a candidate already has one, skip it — never post a second time on the same issue. Stop after 30 comments in a run.

## Steps

**1 — Load candidates.** Read `issue-candidates.json`: a JSON array, most-referenced first, each entry `{number, url, title, refs}` where `refs` lists the ``file:line`` locations. If it is missing, empty, or `[]`, skip to *Nothing to do*. Otherwise work through it in order.

**2 — Confirm each case.** Pull the issue through the `github` MCP tools and check it is really still closed. The close reason tells you what to recommend: an issue closed as stale or inactive while the code still guards against it points toward reopening; one closed as fixed, by-design, duplicate, or won't-fix points toward removing the now-stale reference. When it is genuinely ambiguous, present both options rather than asserting one. Drop anything that no longer holds — already reopened, unrelated reference — with a short `-> skipped: <reason>`. A false advisory is worse than a missed one.

**3 — Deduplicate.** Read the issue's comments and skip it if the marker is already present. An issue that has already been advised gets nothing this run.

**4 — Comment.** For each surviving candidate, post one `add-comment` in this shape, filling `Referenced at` from the candidate's `refs`:

```markdown
<!-- closed-issue-reference-check:advice -->
**Closed issue still referenced in code:** this issue is closed but its number still appears in the source tree, so it should be **reopened** — or the references removed if it was closed intentionally.

**Referenced at:**
- `path/to/File.cs:123`
- `path/to/Other.cs:45`

_Automated suggestion; a maintainer makes the call._
```

## Nothing to do

If the file was empty, or every candidate was dropped or already advised, call `noop` instead of commenting:

```json
{"noop": {"message": "No reopen recommendations: [brief explanation]"}}
```
