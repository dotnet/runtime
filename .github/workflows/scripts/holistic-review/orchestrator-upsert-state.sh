#!/usr/bin/env bash
set -euo pipefail

state_issue_comment_prefix='<!-- holistic-review-orchestrator-state:pr-'
state_issue_comment_pattern='^<!-- holistic-review-orchestrator-state:pr-[1-9][0-9]* -->\n'

state_issue_number="$STATE_ISSUE_NUMBER"

state_comment="$(
  gh api --method GET --paginate --slurp \
    "repos/${GITHUB_REPOSITORY}/issues/${state_issue_number}/comments?per_page=100" |
    jq -c --arg pr_number "$PR_NUMBER" '
      [
        .[][]
        | select(
            .user.login == "github-actions[bot]"
            and ((.body // "") | startswith("<!-- holistic-review-orchestrator-state:pr-" + $pr_number + " -->"))
          )
      ]
      | last // empty
    '
)"
current_pr="$(gh pr view "$PR_NUMBER" --repo "$GITHUB_REPOSITORY" --json headRefOid,baseRefName)"
current_pr_head="$(jq -r '.headRefOid' <<< "$current_pr")"
current_pr_base_ref="$(jq -r '.baseRefName' <<< "$current_pr")"
if [ -n "$state_comment" ]; then
  current_state_json="$(
    jq -er '
      .body
      | split("```json\n")[1]
      | split("\n```")[0]
      | fromjson
    ' <<< "$state_comment"
  )"
  # Do not let a delayed dispatch overwrite a completed callback for the current PR head/base.
  if [ "$(jq -r '.last_reviewed_commit // ""' <<< "$current_state_json")" = "$current_pr_head" ] &&
     [ "$(jq -r '.last_reviewed_base_ref // ""' <<< "$current_state_json")" = "$current_pr_base_ref" ] &&
     { [ "$(jq -r '.last_reviewed_commit // ""' <<< "$STATE_JSON")" != "$current_pr_head" ] ||
       [ "$(jq -r '.last_reviewed_base_ref // ""' <<< "$STATE_JSON")" != "$current_pr_base_ref" ]; }; then
    STATE_JSON="$current_state_json"
  fi
fi
pr_title_html="$(jq -rn --arg text "$PR_TITLE" '
  $text
  | gsub("&"; "&amp;")
  | gsub("<"; "&lt;")
  | gsub(">"; "&gt;")
  | gsub("\""; "&quot;")
')"
review_status_emoji="$(jq -r '
  if [
       .last_dispatched_commit // "",
       .last_dispatched_base_ref // ""
     ] == [
       .last_reviewed_commit // "",
       .last_reviewed_base_ref // ""
     ]
  then ":heavy_check_mark:"
  else ":eyes:"
  end
' <<< "$STATE_JSON")"
state_body="$(
  printf '%s%s -->\n<details>\n<summary>%s <a href="https://redirect.github.com/%s/pull/%s">#%s - %s</a></summary>\n\n```json\n%s\n```\n</details>' \
    "$state_issue_comment_prefix" \
    "$PR_NUMBER" \
    "$review_status_emoji" \
    "$GITHUB_REPOSITORY" \
    "$PR_NUMBER" \
    "$PR_NUMBER" \
    "$pr_title_html" \
    "$STATE_JSON"
)"
if [ -n "$state_comment" ]; then
  gh api --method PATCH \
    "repos/${GITHUB_REPOSITORY}/issues/comments/$(jq -er '.id' <<< "$state_comment")" \
    -f "body=${state_body}" > /dev/null
else
  gh api --method POST \
    "repos/${GITHUB_REPOSITORY}/issues/${state_issue_number}/comments" \
    -f "body=${state_body}" > /dev/null
fi

if [ -n "${WORKER_DISPATCH:-}" ] && [ "$WORKER_DISPATCH" != 'null' ]; then
  worker_pr_number="$(jq -er '.pr_number' <<< "$WORKER_DISPATCH")"
  worker_base_ref="$(jq -er '.base_ref' <<< "$WORKER_DISPATCH")"
  worker_head_sha="$(jq -er '.head_sha' <<< "$WORKER_DISPATCH")"
  worker_previous_head_sha="$(jq -er '.previous_head_sha' <<< "$WORKER_DISPATCH")"
  worker_previous_base_sha="$(jq -er '.previous_base_sha' <<< "$WORKER_DISPATCH")"
  worker_review_history="$(jq -er '.review_history' <<< "$WORKER_DISPATCH")"
  worker_fetch_sha="$(jq -er '.fetch_sha' <<< "$WORKER_DISPATCH")"
  worker_aw_context="$(jq -er '.aw_context' <<< "$WORKER_DISPATCH")"

  gh api --method POST \
    "repos/${GITHUB_REPOSITORY}/actions/workflows/holistic-review.lock.yml/dispatches" \
    -f "ref=${DEFAULT_BRANCH}" \
    -f "inputs[pr_number]=${worker_pr_number}" \
    -f "inputs[pr_base_ref]=${worker_base_ref}" \
    -f "inputs[pr_head_sha]=${worker_head_sha}" \
    -f "inputs[previous_head_sha]=${worker_previous_head_sha}" \
    -f "inputs[previous_base_sha]=${worker_previous_base_sha}" \
    -f "inputs[previous_review_history]=${worker_review_history}" \
    -f "inputs[fetch_sha]=${worker_fetch_sha}" \
    -f "inputs[aw_context]=${worker_aw_context}" > /dev/null
fi
