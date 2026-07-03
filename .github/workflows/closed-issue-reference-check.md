---
name: "Closed Issue Reference Check"
description: "Periodic check that flags closed issues still used to disable or guard code — an ActiveIssue attribute, a Skip, or a project-exclusion comment — where the code leans on the issue link instead of stating the reason. A deterministic pre-step collects those references; the agent reads the surrounding code and advises making it self-describing, or reopening the issue if it was not actually fixed."

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
  model: claude-opus-4.8
  env:
    COPILOT_GITHUB_TOKEN: ${{ case(needs.pat_pool.outputs.pat_number == '0', secrets.COPILOT_PAT_0, needs.pat_pool.outputs.pat_number == '1', secrets.COPILOT_PAT_1, needs.pat_pool.outputs.pat_number == '2', secrets.COPILOT_PAT_2, needs.pat_pool.outputs.pat_number == '3', secrets.COPILOT_PAT_3, needs.pat_pool.outputs.pat_number == '4', secrets.COPILOT_PAT_4, needs.pat_pool.outputs.pat_number == '5', secrets.COPILOT_PAT_5, needs.pat_pool.outputs.pat_number == '6', secrets.COPILOT_PAT_6, needs.pat_pool.outputs.pat_number == '7', secrets.COPILOT_PAT_7, needs.pat_pool.outputs.pat_number == '8', secrets.COPILOT_PAT_8, needs.pat_pool.outputs.pat_number == '9', secrets.COPILOT_PAT_9, secrets.COPILOT_GITHUB_TOKEN) }}

concurrency:
  group: "closed-issue-reference-check"
  cancel-in-progress: true

tools:
  bash: ["git", "find", "ls", "cat", "grep", "head", "tail", "wc", "jq", "tee", "sed", "awk", "tr", "cut", "sort", "uniq", "xargs", "echo", "date", "mkdir", "test", "env", "basename", "dirname"]

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

      # Find closed issues still used to disable or guard code (ActiveIssue, Skip, or a
      # project-exclusion comment) under src, tagged by construct, into issue-candidates.json.

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
      raw="$(mktemp)"
      # Match issue-URL links in source and build files. Keep the full matched line
      # (not grep -o) so awk can pull every issue number on it and classify the
      # construct the link sits in. grep exits 1 on no matches, which is not an error.
      grep -rEnI "${owner}/${name}/issues/[0-9]+" $DIRS \
        --include=*.cs --include=*.csproj --include=*.proj --include=*.props --include=*.targets 2>/dev/null > "$raw" \
        || { rc=$?; [ "$rc" -eq 1 ] || exit "$rc"; }
      awk -v pat="${owner}/${name}/issues/" '
          {
            split($0, a, ":"); path=a[1]; lineno=a[2];
            kind = "reference";
            if ($0 ~ /ActiveIssue/) kind = "ActiveIssue";
            else if ($0 ~ /Skip[ \t]*=/) kind = "Skip";
            else if (path ~ /\.(csproj|proj|props|targets)$/ && $0 ~ /<!--/) kind = "build-exclusion";
            s = $0;
            while (match(s, pat "[0-9]+")) {
              n = substr(s, RSTART, RLENGTH); sub(".*/issues/", "", n);
              if (n != "") print n "\t" path ":" lineno "\t" kind;
              s = substr(s, RSTART + RLENGTH);
            }
          }' "$raw" > "$refs"
      rm -f "$raw"

      # Group references by issue number, keep only issues that appear in at least one
      # disabling construct, and rank by reference count. State is resolved only for the
      # top numbers instead of every one (which can be very large in a big repo).
      grouped="$(mktemp)"
      jq -R 'split("\t") | {number:(.[0]|tonumber), location:.[1], kind:.[2]}' "$refs" \
        | jq -s 'sort_by(.number) | group_by(.number)
                 | map({ number: .[0].number,
                         kinds: ([.[].kind] | unique),
                         refs:  ([.[] | {location, kind}] | unique) })
                 | map(select(.kinds | any(. == "ActiveIssue" or . == "Skip" or . == "build-exclusion")))
                 | map(.count = (.refs | length))
                 | sort_by(-.count)' > "$grouped"

      probe_nums="$(mktemp)"
      jq -r --argjson probe "$(( MAX * 10 ))" '.[0:$probe][].number' "$grouped" > "$probe_nums"
      echo "scan: $(jq 'length' "$grouped") referenced issue numbers under ${DIRS}; resolving state for top $(wc -l < "$probe_nums" | tr -d ' ')"

      states="$(mktemp)"
      emit_query() {
        local parts="$1"
        [ -z "$parts" ] && return 0
        local q="query{repository(owner:\"$owner\",name:\"$name\"){ ${parts} }}"
        # issueOrPullRequest with an `... on Issue` fragment resolves PR numbers to an
        # empty object rather than erroring, so no per-alias failures for PR links.
        # Still verify `.data.repository` came back and abort loudly if not, rather
        # than silently yielding an empty set and a false "nothing found".
        local resp
        resp="$(gh api graphql -f query="$q" 2>/dev/null || true)"
        if ! printf '%s' "$resp" | jq -e '.data.repository' >/dev/null 2>&1; then
          echo "scan: GitHub GraphQL returned no repository data (auth/rate-limit/transient); aborting to avoid false negatives" >&2
          exit 1
        fi
        printf '%s' "$resp" | jq -c '.data.repository | to_entries[] | .value | select(. != null and .state != null)' >> "$states"
      }
      parts=""; cnt=0
      while read -r n; do
        [ -z "$n" ] && continue
        parts+="i${n}: issueOrPullRequest(number:${n}){ ... on Issue { number state title url } } "
        cnt=$((cnt+1))
        if [ "$cnt" -ge 100 ]; then emit_query "$parts"; parts=""; cnt=0; fi
      done < "$probe_nums"
      emit_query "$parts"

      ranked="$(mktemp)"
      jq -s --slurpfile grouped "$grouped" '
        ([.[] | select(.state=="CLOSED")]) as $closed
        | ($grouped[0]) as $g
        | [ $closed[] | . as $c
            | ($g[] | select(.number==$c.number)) as $r
            | select($r != null)
            | { number:$c.number, url:$c.url, title:$c.title,
                refs:($r.refs[0:15]), _count:$r.count } ]
        | sort_by(-._count) | map(del(._count))
      ' "$states" > "$ranked"

      # Drop issues that already carry the advisory marker so the agent never has to
      # reason about ones it would only skip, keeping up to MAX new candidates.
      marker="<!-- closed-issue-reference-check:advice -->"
      keptnums="$(mktemp)"; keptn=0
      while read -r num; do
        [ -z "$num" ] && continue
        [ "$keptn" -ge "$MAX" ] && break
        comments="$(gh api "repos/${REPO}/issues/${num}/comments" --paginate -q '.[].body' 2>/dev/null || true)"
        if printf '%s' "$comments" | grep -qF "$marker"; then
          echo "scan: #${num} already advised -> filtered"
          continue
        fi
        echo "$num" >> "$keptnums"; keptn=$((keptn+1))
      done < <(jq -r '.[].number' "$ranked")

      keptjson="$(jq -R 'tonumber' "$keptnums" | jq -s '.')"
      jq --argjson keep "$keptjson" 'map(select(.number as $n | ($keep | index($n)) != null))' "$ranked" > "$OUT"

      rm -f "$refs" "$grouped" "$probe_nums" "$states" "$ranked" "$keptnums"
      count="$(jq 'length' "$OUT")"
      echo "scan: ${count} closed issue(s) still referenced and not yet advised -> ${OUT}"
      jq -r '.[] | "  #\(.number) (\(.refs|length) refs) \(.title)"' "$OUT" || true

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

You review how closed issues are used to disable or guard code. A deterministic step has scanned the source and build files under `src` for issue-URL links that sit in a test-disabling or guarding construct — an `[ActiveIssue(...)]` attribute, a `Skip = ...`, or a project-exclusion comment — and written `issue-candidates.json` to the workspace root. Every entry is a closed issue still used this way. There are two problems to catch, and which one applies depends on the construct. An `[ActiveIssue(...)]` must only ever reference an *active* (open) issue, so an `ActiveIssue` pointing at a closed issue is always wrong — the issue should be reopened or the attribute removed and the test re-enabled, and no comment changes that. A `Skip` or a project exclusion may legitimately reference a closed issue, but the code should state, in the code itself, why the test is disabled; leaning on the issue link instead of a stated reason is the problem there. The reason belongs in the code so it is self-describing; an issue link is not a substitute for it, and is warranted only in the rare case a comment cannot capture.

You only suggest; you never act. The one write you can make is an `add-comment` through `safe-outputs`. Do not change issue state or edit files.

## Guardrails

- **Work from `issue-candidates.json` only.** The scan already collected the references and the construct each one sits in, so never grow the candidate set. The issue's own content is not the point — the code around each reference is.
- **Judge by construct.** An `ActiveIssue` reference to a closed issue is always a finding — flag it regardless of any nearby comment, because the attribute must point at an active issue. For a `Skip` or a `build-exclusion`, read the lines around the reference and flag only where the issue link is the sole explanation; a reference that already carries a clear reason next to it is fine, leave it.
- **One comment per issue, ever.** Each comment carries the hidden marker `<!-- closed-issue-reference-check:advice -->`. The scan already removed issues that have this marker, so the candidate set is free of already-advised issues; never post a second time on the same issue. Stop after 30 comments in a run.

## Steps

**1 — Load candidates.** Read `issue-candidates.json`: a JSON array, most-referenced first, each entry `{number, url, title, refs}` where each ref is `{location: "file:line", kind}` and `kind` is `ActiveIssue`, `Skip`, or `build-exclusion`. If it is missing, empty, or `[]`, skip to *Nothing to do*. Otherwise work through it in order.

**2 — Judge each reference by its construct.** For an `ActiveIssue` reference, the finding is automatic: the attribute points at a closed issue, which is wrong no matter what surrounds it, so keep it. For a `Skip` or a `build-exclusion`, open the file and read the few lines around `location`, then ask whether the code states, on its own, why the test is disabled or guarded, or whether the issue link is the only explanation. An exclusion or `Skip` annotated only with the issue link is not self-describing; a nearby comment that states the actual failure or condition is — drop those. Drop an issue only when every one of its references is either such a self-describing `Skip`/`build-exclusion` or is unrelated or stale, with a short `-> skipped: <reason>`. A false advisory is worse than a missed one.

**3 — Comment.** For each issue that still has at least one flagged reference — any `ActiveIssue`, or a link-only `Skip`/`build-exclusion` — post one `add-comment` in this shape, listing those references and their `kind`:

```markdown
<!-- closed-issue-reference-check:advice -->
**Closed issue referenced by a test-disabling or guarding construct.** An `[ActiveIssue(...)]` must only reference an active issue, so if one points here the issue should be reopened or the attribute removed and the test re-enabled. A `Skip` or project exclusion should state its reason in the code rather than rely on the issue link — make it self-describing, or reopen the issue if it was not actually fixed.

**Referenced at:**
- `path/to/File.cs:123` — ActiveIssue
- `path/to/tests.proj:370` — build-exclusion

_Automated suggestion; a maintainer makes the call._
```

## Nothing to do

If the file was empty, or every candidate was already self-describing or dropped, call `noop` instead of commenting:

```json
{"noop": {"message": "No recommendations: [brief explanation]"}}
```
