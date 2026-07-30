#!/usr/bin/env bash
set -euo pipefail

state_issue_title='[Holistic Review Orchestrator] PR Review State'
state_issue_body='<!-- holistic-review-orchestrator-state-store -->

This issue stores state for pull request reviews conducted by the Holistic Review workflows.'
state_issue_comment_pattern='^<!-- holistic-review-orchestrator-state:pr-[1-9][0-9]* -->\n'

create_state_issue() {
  gh api --method POST \
    "repos/${GITHUB_REPOSITORY}/issues" \
    -f "title=${state_issue_title}" \
    -f "body=${state_issue_body}" |
    jq -er '.number'
}

state_issue_search="$(
  jq -rn \
    --arg repository "$GITHUB_REPOSITORY" \
    --arg state_issue_title "$state_issue_title" '
      "repo:\($repository) is:issue in:title \"\($state_issue_title)\""
      | @uri
    '
)"
state_issue="$(
  gh api --method GET --paginate --slurp \
    "search/issues?q=${state_issue_search}&per_page=100" |
    jq -c --arg state_issue_title "$state_issue_title" '
      [
        .[] | .items[]
        | select(.user.login == "github-actions[bot]")
        | select(.title == $state_issue_title)
      ]
      | sort_by(.number)
      | last // empty
    '
)"

if [ "${CREATE_STATE_ISSUE:-true}" != true ]; then
    # Completion callbacks only consume an open store. The globally serialized dispatcher
    # creates and rotates it, preventing concurrent callbacks from creating duplicates.
    if [ -n "$state_issue" ] && [ "$(jq -r '.state' <<< "$state_issue")" = "open" ]; then
      state_issue_number="$(jq -er '.number' <<< "$state_issue")"
    else
      state_issue_number=''
    fi
elif [ -z "$state_issue" ]; then
    state_issue_number="$(create_state_issue)"
elif [ "$(jq -r '.state' <<< "$state_issue")" = "open" ]; then
  state_issue_number="$(jq -er '.number' <<< "$state_issue")"
else
  # A closed store is immutable; seed its replacement with active PR state only.
  closed_state_issue_number="$(jq -er '.number' <<< "$state_issue")"
  state_issue_number="$(create_state_issue)"
  active_pr_numbers="$(
    gh pr list --repo "$GITHUB_REPOSITORY" --state open --limit 1000 --json number --jq '[.[].number]'
  )"
  closed_state_comments="$(
    gh api --method GET --paginate --slurp \
      "repos/${GITHUB_REPOSITORY}/issues/${closed_state_issue_number}/comments?per_page=100"
  )"
  while IFS= read -r closed_state_comment; do
    gh api --method POST \
      "repos/${GITHUB_REPOSITORY}/issues/${state_issue_number}/comments" \
      -f "body=$(jq -r '.body' <<< "$closed_state_comment")" > /dev/null
  done < <(
    jq -c \
      --arg state_issue_comment_pattern "$state_issue_comment_pattern" \
      --argjson active_pr_numbers "$active_pr_numbers" '
        [
          .[][]
          | select(
              .user.login == "github-actions[bot]"
              and ((.body // "") | test($state_issue_comment_pattern))
            )
          | . + {
              pr_number: (
                .body
                | capture("^<!-- holistic-review-orchestrator-state:pr-(?<number>[1-9][0-9]*) -->")
                | .number
                | tonumber
              )
            }
          | select(
              .pr_number as $pr_number
              | any($active_pr_numbers[]; . == $pr_number)
            )
        ]
        | sort_by(.pr_number, .updated_at)
        | group_by(.pr_number)
        | map(last)
        | .[]
      ' <<< "$closed_state_comments"
  )
fi

echo "state_issue_number=$state_issue_number" >> "$GITHUB_OUTPUT"
