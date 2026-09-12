#!/usr/bin/env bash

set -euo pipefail

: "${RUNNER_TEMP:?RUNNER_TEMP must be set}"
: "${GITHUB_OUTPUT:?GITHUB_OUTPUT must be set}"

gh api --method GET repos/dotnet/runtime/issues \
  -f state=open \
  -f labels="Known Build Error" \
  -f sort=created \
  -f direction=asc \
  -f per_page=100 \
  --paginate \
  --jq '[
    .[]
    | select(
        .pull_request == null and
        .state == "open" and
        .user.login == "github-actions[bot]" and
        .user.type == "Bot" and
        (.title | startswith("[ci-scan] ")) and
        ([.labels[].name] | index("Known Build Error") != null)
      )
    | {
        number,
        created_at,
        title,
        author: .user.login,
        author_type: .user.type,
        labels: [.labels[].name]
      }
  ]' \
  | jq -cs '{candidates: (add | sort_by(.created_at, .number))}' \
  > "$RUNNER_TEMP/scanner-kbe-candidates.json"

candidate_count="$(jq '.candidates | length' "$RUNNER_TEMP/scanner-kbe-candidates.json")"
echo "Filtered ${candidate_count} scanner-authored KBE(s)."
{
  printf 'count=%s\n' "$candidate_count"
} >> "$GITHUB_OUTPUT"
