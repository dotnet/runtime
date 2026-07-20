#!/usr/bin/env bash
set -euo pipefail

case "$OUTCOME" in
  review-submitted|assessment-unchanged|'')
    ;;
  *)
    echo "Unsupported worker callback outcome: ${OUTCOME}" >&2
    exit 1
    ;;
esac

state_issue_number="$STATE_ISSUE_NUMBER"
state_comments="$(
  gh api --method GET --paginate --slurp \
    "repos/${GITHUB_REPOSITORY}/issues/${state_issue_number}/comments?per_page=100"
)"
state_comment="$(
  jq -c --arg pr_number "$PR_NUMBER" '
    [
      .[][]
      | select(
          .user.login == "github-actions[bot]"
          and ((.body // "") | startswith("<!-- holistic-review-orchestrator-state:pr-" + $pr_number + " -->"))
        )
    ]
    | last // empty
  ' <<< "$state_comments"
)"
if [ -z "$state_comment" ]; then
  echo "No state comment exists for pull request ${PR_NUMBER}." >&2
  exit 1
fi
state_json="$(
  jq -er '
    .body
    | split("```json\n")[1]
    | split("\n```")[0]
    | fromjson
  ' <<< "$state_comment"
)"
pr="$(
  gh pr view "$PR_NUMBER" --repo "$GITHUB_REPOSITORY" \
    --json number,title,baseRefName,baseRefOid,headRefOid
)"
if [ "$(jq -r '.headRefOid' <<< "$pr")" != "$HEAD_SHA" ]; then
  echo "Callback head SHA does not match the current pull request head." >&2
  exit 1
fi

find_current_review() {
  gh api --paginate --slurp \
    "repos/${GITHUB_REPOSITORY}/pulls/${PR_NUMBER}/reviews?per_page=100" |
    jq -c --arg head_sha "$HEAD_SHA" '
      [
        .[][]
        | select(
            .user.login == "github-actions[bot]"
            and .state == "COMMENTED"
            and .commit_id == $head_sha
            and ((.body // "") | contains("<!-- gh-aw-agentic-workflow: Holistic Review,"))
          )
      ]
      | sort_by(.submitted_at)
      | last
    '
}

review_history="$(jq -c '.review_history // []' <<< "$state_json")"
if [ -z "$OUTCOME" ]; then
  current_review=''
  for _ in {1..12}; do
    current_review="$(find_current_review)"
    if [ "$current_review" != "null" ]; then
      break
    fi
    sleep 5
  done
  if [ "$current_review" != "null" ] && [ -n "$current_review" ]; then
    OUTCOME='review-submitted'
  elif [ "$(jq 'length' <<< "$review_history")" -gt 0 ]; then
    OUTCOME='assessment-unchanged'
  else
    echo "Worker callback did not identify a submitted review or a prior assessment." >&2
    exit 1
  fi
fi

case "$OUTCOME" in
  review-submitted)
    # The review write and callback are separate workflow steps, so wait for GitHub to expose it.
    current_review=''
    for _ in {1..12}; do
      current_review="$(find_current_review)"
      if [ "$current_review" != "null" ]; then
        break
      fi
      sleep 5
    done
    if [ "$current_review" = "null" ] || [ -z "$current_review" ]; then
      echo "The worker callback reported a submitted review that could not be found." >&2
      exit 1
    fi
    review_history="$(
      jq -cn \
        --argjson review_history "$review_history" \
        --argjson current_review "$current_review" '
          ($current_review | { commit: .commit_id, review_id: .id }) as $review
          | if ($review_history | length) == 0 then
              [$review]
            elif $review_history[0].review_id == $review.review_id then
              [$review]
            else
              [$review_history[0], $review]
            end
        '
    )"
    ;;
  assessment-unchanged)
    # The worker reused the prior physical review; only its covered commit advances.
    review_history="$(
      jq -c --arg head_sha "$HEAD_SHA" '
        if length == 0 then
          error("An unchanged assessment requires a prior Holistic Review.")
        else
          .[-1].commit = $head_sha
        end
      ' <<< "$review_history"
    )"
    ;;
  *)
    echo "Unsupported worker callback outcome: ${OUTCOME}" >&2
    exit 1
    ;;
esac

state_json="$(
  jq -c \
    --arg head_sha "$HEAD_SHA" \
    --arg base_ref "$(jq -r '.baseRefName' <<< "$pr")" \
    --arg base_sha "$(jq -r '.baseRefOid' <<< "$pr")" \
    --arg title "$(jq -r '.title' <<< "$pr")" \
    --argjson review_history "$review_history" '
      .version = 8
      | .last_dispatched_commit = $head_sha
      | .last_dispatched_base_ref = $base_ref
      | .last_dispatched_base_sha = $base_sha
      | .last_reviewed_commit = $head_sha
      | .last_reviewed_base_ref = $base_ref
      | .last_reviewed_base_sha = $base_sha
      | .pull_request_title = $title
      | .review_attempt_commit = ""
      | .review_attempt_base_ref = ""
      | .review_attempt_count = 0
      | .review_history_format = "holistic-review-disclosure-v1"
      | .review_history = $review_history
      | del(.last_recorded_worker_run_id)
    ' <<< "$state_json"
)"
state_updates="$(jq -cn \
  --argjson pr_number "$PR_NUMBER" \
  --arg title "$(jq -r '.title' <<< "$pr")" \
  --argjson state "$state_json" \
  '{ include: [{ pr_number: $pr_number, title: $title, state: $state }] }'
)"
{
  echo "state_updates=$state_updates"
} >> "$GITHUB_OUTPUT"
