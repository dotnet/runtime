#!/usr/bin/env bash
set -euo pipefail

open_prs_file="$(mktemp)"
dispatched_prs_file="$(mktemp)"
retry_limited_prs_file="$(mktemp)"
already_reviewed_prs_file="$(mktemp)"
closed_prs_file="$(mktemp)"
state_updates_file="$(mktemp)"
trap 'rm -f "$open_prs_file" "$dispatched_prs_file" "$retry_limited_prs_file" "$already_reviewed_prs_file" "$closed_prs_file" "$state_updates_file"' EXIT
state_issue_comment_prefix='<!-- holistic-review-orchestrator-state:pr-'
state_issue_comment_pattern='^<!-- holistic-review-orchestrator-state:pr-[1-9][0-9]* -->\n'

requested_pr_numbers='[]'
if [ -n "$PR_NUMBERS" ]; then
  requested_pr_numbers="$(jq -Rn --arg pr_numbers "$PR_NUMBERS" '
    $pr_numbers
    | split(",")
    | map(gsub("^\\s+|\\s+$"; ""))
    | if any(.[]; test("^[1-9][0-9]*$") | not) then
        error("pr_numbers must be a comma-separated list of positive pull request numbers")
      else
        map(tonumber) | unique
      end
  ')"
fi

gh pr list --repo "$GITHUB_REPOSITORY" --state open --limit 1000 \
  --json number,title,baseRefName,baseRefOid,headRefOid,isDraft,updatedAt \
  --jq '[.[]]' > "$open_prs_file"
# Capture open pull requests before filtering drafts so their state comments are retained.
active_pr_numbers="$(jq -c '[.[] | .number]' "$open_prs_file")"
if ! jq -e --argjson requested_pr_numbers "$requested_pr_numbers" '
  if ($requested_pr_numbers | length) == 0 then
    true
  else
    ([.[] | .number] as $open_pr_numbers
     | all($requested_pr_numbers[]; . as $requested | any($open_pr_numbers[]; . == $requested)))
  end
' "$open_prs_file" > /dev/null; then
  echo "One or more requested pull requests are not open." >&2
  exit 1
fi
jq --argjson requested_pr_numbers "$requested_pr_numbers" '
  if ($requested_pr_numbers | length) == 0 then
    .
  else
    [.[] | select(.number as $number | any($requested_pr_numbers[]; . == $number))]
  end
' "$open_prs_file" > "${open_prs_file}.filtered"
if [ "$(jq 'length' <<< "$requested_pr_numbers")" -eq 0 ]; then
  jq '[.[] | select(.isDraft == false)]' "${open_prs_file}.filtered" > "$open_prs_file"
else
  mv "${open_prs_file}.filtered" "$open_prs_file"
fi
echo "Eligible pull requests: $(jq 'length' "$open_prs_file")"

state_issue_number="$STATE_ISSUE_NUMBER"
state_comments="$(
  gh api --method GET --paginate --slurp \
    "repos/${GITHUB_REPOSITORY}/issues/${state_issue_number}/comments?per_page=100"
)"

html_escape() {
  jq -rn --arg text "$1" '
    $text
    | gsub("&"; "&amp;")
    | gsub("<"; "&lt;")
    | gsub(">"; "&gt;")
    | gsub("\""; "&quot;")
  '
}

while IFS= read -r resolved_state_comment; do
  resolved_state_comment_id="$(jq -er '.id' <<< "$resolved_state_comment")"
  closed_pr_number="$(jq -er '
    .body
    | capture("^<!-- holistic-review-orchestrator-state:pr-(?<number>[1-9][0-9]*) -->")
    | .number
  ' <<< "$resolved_state_comment")"
  gh api --method DELETE \
    "repos/${GITHUB_REPOSITORY}/issues/comments/${resolved_state_comment_id}" > /dev/null
  printf '| [#%s](%s/%s/pull/%s) |\n' \
    "$closed_pr_number" \
    "$GITHUB_SERVER_URL" \
    "$GITHUB_REPOSITORY" \
    "$closed_pr_number" >> "$closed_prs_file"
done < <(
  # Remove state for closed PRs so the central issue remains bounded over time.
  jq -c \
    --arg state_issue_comment_pattern "$state_issue_comment_pattern" \
    --argjson active_pr_numbers "$active_pr_numbers" '
      .[][]
      | select(
          .user.login == "github-actions[bot]"
          and ((.body // "") | test($state_issue_comment_pattern))
          and (
            .body
            | capture("^<!-- holistic-review-orchestrator-state:pr-(?<number>[1-9][0-9]*) -->")
            | .number
            | tonumber as $pr_number
            | any($active_pr_numbers[]; . == $pr_number)
            | not
          )
      )
    ' <<< "$state_comments"
)

worker_runs_since="$(date -u -d '7 days ago' '+%Y-%m-%dT%H:%M:%SZ')"
worker_runs="$(
  gh api --method GET --paginate --slurp \
    "repos/${GITHUB_REPOSITORY}/actions/workflows/holistic-review.lock.yml/runs" \
    -f per_page=100 \
    -f "created=>=${worker_runs_since}" |
    jq -c '{ workflow_runs: [ .[] | .workflow_runs[] ] }'
)"

get_review_history() {
  local pr_number="$1"
  gh api --paginate --slurp \
    "repos/${GITHUB_REPOSITORY}/pulls/${pr_number}/reviews?per_page=100" |
    jq -c '
      [
        .[][]
        | select(
            .user.login == "github-actions[bot]"
            and .state == "COMMENTED"
            and ((.body // "") | contains("<!-- gh-aw-agentic-workflow: Holistic Review,"))
          )
        | . as $review
        | (
              (
              ($review.body // "")
              | capture(
                  "<!-- holistic-review-reassessment:sha:(?<sha>[0-9a-f]{40}),base:[0-9a-f]{40} -->"
                  )?
              | .sha
              ) // $review.commit_id
          ) as $reviewed_commit
        | {
            commit: $reviewed_commit,
            review_id: $review.id,
            submitted_at: $review.submitted_at
          }
      ]
      | sort_by(.submitted_at)
      | reduce .[] as $review (
          [];
          if any(.[]; .review_id == $review.review_id)
          then .
          else . + [$review]
          end
        )
      # The first review anchors the initial assessment; the latest is the current assessment.
      | if length > 1 then [.[0], .[-1]] else . end
      | map(del(.submitted_at))
    '
}

dispatched=0
while IFS= read -r entry; do
  pr_number="$(jq -er '.pr_number' <<< "$entry")"
  pr_title="$(jq -er '.title' <<< "$entry")"
  pr_title_html="$(html_escape "$pr_title")"
  base_ref="$(jq -er '.base_ref' <<< "$entry")"
  base_sha="$(jq -er '.base_sha' <<< "$entry")"
  head_sha="$(jq -er '.head_sha' <<< "$entry")"

  state_comment="$(jq -c \
    --arg state_issue_comment_prefix "$state_issue_comment_prefix" \
    --arg pr_number "$pr_number" '
      [
        .[][]
        | select(
            .user.login == "github-actions[bot]"
            and (
              (.body // "")
              | startswith($state_issue_comment_prefix + $pr_number + " -->")
            )
          )
      ]
      | last // empty
    ' <<< "$state_comments")"
  last_dispatched_commit=''
  last_dispatched_base_ref=''
  last_dispatched_base_sha=''
  last_reviewed_commit=''
  last_reviewed_base_ref=''
  last_reviewed_base_sha=''
  review_history='[]'
  review_attempt_commit=''
  review_attempt_base_ref=''
  review_attempt_count=0
  last_no_actionable_commit=''
  last_no_actionable_base_ref=''
  last_no_actionable_base_sha=''
  worker_dispatch='null'
  manual_retry_reset=false
  if [ -n "$state_comment" ]; then
    state_json="$(jq -er '
      .body
      | split("```json\n")[1]
      | split("\n```")[0]
      | fromjson
    ' <<< "$state_comment")"
    last_dispatched_commit="$(jq -r '.last_dispatched_commit // ""' <<< "$state_json")"
    last_dispatched_base_sha="$(jq -r '.last_dispatched_base_sha // ""' <<< "$state_json")"
    last_dispatched_base_ref="$(jq -r '.last_dispatched_base_ref // ""' <<< "$state_json")"
    last_reviewed_commit="$(jq -r '.last_reviewed_commit // ""' <<< "$state_json")"
    last_reviewed_base_sha="$(jq -r '.last_reviewed_base_sha // ""' <<< "$state_json")"
    last_reviewed_base_ref="$(jq -r '.last_reviewed_base_ref // ""' <<< "$state_json")"
    review_history="$(jq -c '.review_history // []' <<< "$state_json")"
    review_attempt_commit="$(jq -r '.review_attempt_commit // ""' <<< "$state_json")"
    review_attempt_base_ref="$(jq -r '.review_attempt_base_ref // ""' <<< "$state_json")"
    review_attempt_count="$(jq -r '.review_attempt_count // 0' <<< "$state_json")"
    last_no_actionable_commit="$(jq -r '.last_no_actionable_commit // ""' <<< "$state_json")"
    last_no_actionable_base_ref="$(jq -r '.last_no_actionable_base_ref // ""' <<< "$state_json")"
    last_no_actionable_base_sha="$(jq -r '.last_no_actionable_base_sha // ""' <<< "$state_json")"
  fi

  write_state_comment() {
    # Emit normalized state for the shared upsert job rather than writing concurrently here.
    state_json="$(
      jq -n \
        --arg last_dispatched_commit "$last_dispatched_commit" \
        --arg last_dispatched_base_ref "$last_dispatched_base_ref" \
        --arg last_dispatched_base_sha "$last_dispatched_base_sha" \
        --arg last_reviewed_commit "$last_reviewed_commit" \
        --arg last_reviewed_base_ref "$last_reviewed_base_ref" \
        --arg last_reviewed_base_sha "$last_reviewed_base_sha" \
        --arg pull_request_title "$pr_title" \
        --arg review_attempt_commit "$review_attempt_commit" \
        --arg review_attempt_base_ref "$review_attempt_base_ref" \
        --argjson review_attempt_count "$review_attempt_count" \
        --argjson max_review_attempts "$MAX_REVIEW_ATTEMPTS" \
        --arg last_no_actionable_commit "$last_no_actionable_commit" \
        --arg last_no_actionable_base_ref "$last_no_actionable_base_ref" \
        --arg last_no_actionable_base_sha "$last_no_actionable_base_sha" \
        --argjson review_history "$review_history" '
          {
            version: 9,
            last_dispatched_commit: $last_dispatched_commit,
            last_dispatched_base_ref: $last_dispatched_base_ref,
            last_dispatched_base_sha: $last_dispatched_base_sha,
            last_reviewed_commit: $last_reviewed_commit,
            last_reviewed_base_ref: $last_reviewed_base_ref,
            last_reviewed_base_sha: $last_reviewed_base_sha,
            pull_request_title: $pull_request_title,
            review_attempt_commit: $review_attempt_commit,
            review_attempt_base_ref: $review_attempt_base_ref,
            review_attempt_count: $review_attempt_count,
            max_review_attempts: $max_review_attempts,
            review_history_format: "holistic-review-disclosure-v1",
            review_history: $review_history,
            last_no_actionable_commit: $last_no_actionable_commit,
            last_no_actionable_base_ref: $last_no_actionable_base_ref,
            last_no_actionable_base_sha: $last_no_actionable_base_sha
          }
        '
    )"
    jq -cn \
      --argjson pr_number "$pr_number" \
      --arg title "$pr_title" \
      --argjson state "$state_json" \
      --argjson worker_dispatch "$worker_dispatch" '
        {
          pr_number: $pr_number,
          title: $title,
          state: $state
        }
        | if $worker_dispatch == null then . else .worker_dispatch = $worker_dispatch end
      ' >> "$state_updates_file"
  }

  # A no-actionable completion has no physical review. Keep its durable completion marker
  # authoritative for this head/base pair instead of deriving an empty review history as pending.
  state_needs_update=false
  has_no_actionable_completion=false
  if [ "$last_no_actionable_commit" = "$head_sha" ] &&
     [ "$last_no_actionable_base_ref" = "$base_ref" ]; then
    has_no_actionable_completion=true
  elif [ -n "$last_no_actionable_commit" ] ||
       [ -n "$last_no_actionable_base_ref" ] ||
       [ -n "$last_no_actionable_base_sha" ]; then
    last_no_actionable_commit=''
    last_no_actionable_base_ref=''
    last_no_actionable_base_sha=''
    state_needs_update=true
  fi
  expected_summary_emoji=':eyes:'
  if [ "$has_no_actionable_completion" = true ] ||
     { [ "$last_dispatched_commit" = "$last_reviewed_commit" ] &&
       [ "$last_dispatched_base_ref" = "$last_reviewed_base_ref" ]; }; then
    expected_summary_emoji=':heavy_check_mark:'
  fi
  if [ -n "$state_comment" ] &&
     ! jq -e --arg expected_summary_emoji "$expected_summary_emoji" \
       '(.body // "") | contains("<summary>" + $expected_summary_emoji + " ")' \
       <<< "$state_comment" > /dev/null; then
    state_needs_update=true
  fi
  if [ "$has_no_actionable_completion" = false ]; then
    refreshed_review_history="$(get_review_history "$pr_number")"
    recorded_review_id="$(jq -r 'if length == 0 then "" else .[-1].review_id end' <<< "$review_history")"
    refreshed_review_id="$(jq -r 'if length == 0 then "" else .[-1].review_id end' <<< "$refreshed_review_history")"
    if [ "$recorded_review_id" = "$refreshed_review_id" ] && [ -n "$recorded_review_id" ]; then
      refreshed_review_history="$(
        jq -cn \
          --argjson review_history "$review_history" \
          --argjson refreshed_review_history "$refreshed_review_history" '
            $refreshed_review_history
            | .[-1].commit = $review_history[-1].commit
          '
      )"
    fi
    if [ "$review_history" != "$refreshed_review_history" ]; then
      review_history="$refreshed_review_history"
      state_needs_update=true
    fi

    refreshed_reviewed_commit="$(jq -r 'if length == 0 then "" else .[-1].commit end' <<< "$review_history")"
    if [ "$last_reviewed_commit" != "$refreshed_reviewed_commit" ]; then
      last_reviewed_commit="$refreshed_reviewed_commit"
      state_needs_update=true
    fi
    if [ -n "$last_reviewed_commit" ]; then
      if [ "$last_reviewed_base_ref" != "$base_ref" ] ||
         [ "$last_reviewed_base_sha" != "$base_sha" ]; then
        last_reviewed_base_ref="$base_ref"
        last_reviewed_base_sha="$base_sha"
        state_needs_update=true
      fi
      if [ -n "$review_attempt_commit" ] ||
         [ -n "$review_attempt_base_ref" ] ||
         [ "$review_attempt_count" -ne 0 ]; then
        review_attempt_commit=''
        review_attempt_base_ref=''
        review_attempt_count=0
        state_needs_update=true
      fi
    elif [ -n "$last_reviewed_base_ref" ] || [ -n "$last_reviewed_base_sha" ]; then
      last_reviewed_base_ref=''
      last_reviewed_base_sha=''
      state_needs_update=true
    fi
  fi

  if [ -n "$PR_NUMBERS" ] &&
     { [ "$last_reviewed_commit" != "$head_sha" ] ||
       [ "$last_reviewed_base_ref" != "$base_ref" ]; } &&
     [ "$review_attempt_commit" = "$head_sha" ] &&
     [ "$review_attempt_base_ref" = "$base_ref" ] &&
     [ "$review_attempt_count" -ge "$MAX_REVIEW_ATTEMPTS" ]; then
    review_attempt_commit=''
    review_attempt_base_ref=''
    review_attempt_count=0
    manual_retry_reset=true
    state_needs_update=true
  fi

  if [ "$has_no_actionable_completion" = true ] ||
     { [ "$last_reviewed_commit" = "$head_sha" ] &&
       [ "$last_reviewed_base_ref" = "$base_ref" ]; }; then
    if [ -n "$review_attempt_commit" ] ||
       [ -n "$review_attempt_base_ref" ] ||
       [ "$review_attempt_count" -ne 0 ]; then
      review_attempt_commit=''
      review_attempt_base_ref=''
      review_attempt_count=0
      state_needs_update=true
    fi
    if [ -n "$PR_NUMBERS" ]; then
      printf '| [#%s](%s/%s/pull/%s) | `%s` |\n' \
        "$pr_number" \
        "$GITHUB_SERVER_URL" \
        "$GITHUB_REPOSITORY" \
        "$pr_number" \
        "$head_sha" >> "$already_reviewed_prs_file"
    fi
    if [ "$state_needs_update" = true ]; then
      write_state_comment
    fi
    continue
  fi

  if [ "$review_attempt_commit" = "$head_sha" ] &&
     [ "$review_attempt_base_ref" = "$base_ref" ] &&
     [ "$review_attempt_count" -ge "$MAX_REVIEW_ATTEMPTS" ]; then
    printf '| [#%s](%s/%s/pull/%s) | `%s` | %s |\n' \
      "$pr_number" \
      "$GITHUB_SERVER_URL" \
      "$GITHUB_REPOSITORY" \
      "$pr_number" \
      "$head_sha" \
      "$review_attempt_count" >> "$retry_limited_prs_file"
    if [ "$state_needs_update" = true ]; then
      write_state_comment
    fi
    continue
  fi

  review_run_name="Holistic Review #${pr_number} (${head_sha})"
  review_run="$(jq -c --arg review_run_name "$review_run_name" '
    [
      .workflow_runs[]
      | select(.display_title == $review_run_name)
    ]
    | sort_by(.created_at)
    | last // empty
  ' <<< "$worker_runs")"
  if [ "$last_dispatched_commit" = "$head_sha" ] &&
     [ "$last_dispatched_base_ref" = "$base_ref" ] &&
     [ -n "$review_run" ]; then
    review_status="$(jq -r '.status' <<< "$review_run")"
    review_conclusion="$(jq -r '.conclusion // ""' <<< "$review_run")"
    if [ "$review_status" != "completed" ]; then
      if [ "$state_needs_update" = true ]; then
        write_state_comment
      fi
      continue
    fi
    if [ "$review_conclusion" = "success" ]; then
      echo "Completed review run for commit ${head_sha} did not update a Holistic Review; retrying."
      last_dispatched_commit=''
      last_dispatched_base_ref=''
      last_dispatched_base_sha=''
      state_needs_update=true
    fi
  fi

  if [ "$dispatched" -ge "$MAX_DISPATCH" ]; then
    if [ "$manual_retry_reset" = true ]; then
      last_dispatched_commit=''
      last_dispatched_base_ref=''
      last_dispatched_base_sha=''
    fi
    if [ "$state_needs_update" = true ]; then
      write_state_comment
    fi
    if [ -n "$PR_NUMBERS" ]; then
      continue
    fi
    break
  fi

  previous_head_sha="$last_reviewed_commit"
  previous_base_sha="$last_reviewed_base_sha"
  if [ -z "$last_reviewed_base_ref" ]; then
    previous_head_sha=''
    previous_base_sha=''
  fi

  fetch_sha="$previous_head_sha"
  if [ -z "$fetch_sha" ]; then
    fetch_sha="$head_sha"
  fi

  aw_context="$(jq -cn \
    --arg run_id "$GITHUB_RUN_ID" \
    --arg repo "$GITHUB_REPOSITORY" \
    --arg workflow_id "$GITHUB_WORKFLOW_REF" \
    --argjson item_number "$pr_number" \
    '{
      run_id: $run_id,
      repo: $repo,
      workflow_id: $workflow_id,
      item_type: "pull_request",
      item_number: $item_number
    }')"

  if [ "$review_attempt_commit" != "$head_sha" ] ||
     [ "$review_attempt_base_ref" != "$base_ref" ]; then
    review_attempt_count=0
  fi
  review_attempt_commit="$head_sha"
  review_attempt_base_ref="$base_ref"
  review_attempt_count=$((review_attempt_count + 1))
  last_dispatched_commit="$head_sha"
  last_dispatched_base_ref="$base_ref"
  last_dispatched_base_sha="$base_sha"
  worker_dispatch="$(jq -cn \
    --arg pr_number "$pr_number" \
    --arg base_ref "$base_ref" \
    --arg head_sha "$head_sha" \
    --arg previous_head_sha "$previous_head_sha" \
    --arg previous_base_sha "$previous_base_sha" \
    --arg review_history "$review_history" \
    --arg fetch_sha "$fetch_sha" \
    --arg aw_context "$aw_context" '
      {
        pr_number: $pr_number,
        base_ref: $base_ref,
        head_sha: $head_sha,
        previous_head_sha: $previous_head_sha,
        previous_base_sha: $previous_base_sha,
        review_history: $review_history,
        fetch_sha: $fetch_sha,
        aw_context: $aw_context
      }
    ')"
  write_state_comment

  previous_commit_display="$previous_head_sha"
  if [ -z "$previous_commit_display" ]; then
    previous_commit_display='_Initial review_'
  else
    previous_commit_display="\`$previous_commit_display\`"
  fi
  printf '| [#%s](%s/%s/pull/%s) | `%s` | %s |\n' \
    "$pr_number" \
    "$GITHUB_SERVER_URL" \
    "$GITHUB_REPOSITORY" \
    "$pr_number" \
    "$head_sha" \
    "$previous_commit_display" >> "$dispatched_prs_file"

  dispatched=$((dispatched + 1))
done < <(jq -c 'sort_by(.updatedAt)[] | {
  pr_number: .number,
  title: .title,
  base_ref: .baseRefName,
  base_sha: .baseRefOid,
  head_sha: .headRefOid
}' "$open_prs_file")

echo "Prepared ${dispatched} holistic review workflow dispatch(es)."
{
  echo '## Holistic Review Orchestrator'
  echo
  if [ -s "$dispatched_prs_file" ]; then
    echo "Prepared ${dispatched} holistic review workflow dispatch(es):"
    echo
    echo '| Pull request | Dispatched commit | Previously reviewed commit |'
    echo '| --- | --- | --- |'
    cat "$dispatched_prs_file"
  else
    echo 'No holistic review workflows were dispatched.'
  fi
  if [ -s "$closed_prs_file" ]; then
    echo
    echo '### Closed pull requests'
    echo
    echo 'Removed centralized review state comments for these closed pull requests:'
    echo
    echo '| Pull request |'
    echo '| --- |'
    cat "$closed_prs_file"
  fi
  if [ -s "$retry_limited_prs_file" ]; then
    echo
    echo '### Retry limit reached'
    echo
    echo '| Pull request | Commit | Attempts |'
    echo '| --- | --- | ---: |'
    cat "$retry_limited_prs_file"
    echo
    echo "Scheduled retries stop after ${MAX_REVIEW_ATTEMPTS} attempts for one commit and target branch. A targeted manual dispatch resets that review target's retry budget."
  fi
  if [ -s "$already_reviewed_prs_file" ]; then
    echo
    echo '### Already reviewed'
    echo
    echo 'These targeted pull requests already have a durable review for their current commit and target branch, so no duplicate review was dispatched.'
    echo
    echo '| Pull request | Commit |'
    echo '| --- | --- |'
    cat "$already_reviewed_prs_file"
  fi
} >> "$GITHUB_STEP_SUMMARY"

{
  jq -sc '
    sort_by(.pr_number)
    | group_by(.pr_number)
    | map(last)
    | { include: . }
  ' "$state_updates_file" | tr -d '\n' | sed 's/^/state_updates=/'
} >> "$GITHUB_OUTPUT"
