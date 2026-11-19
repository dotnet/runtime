# Issue-Labeler Workflows

This repository uses actions from [dotnet/issue-labeler](https://github.com/dotnet/issue-labeler) to predict area labels for issues and pull requests.

The following workflow templates were imported and updated from [dotnet/issue-labeler/wiki/Onboarding](https://github.com/dotnet/issue-labeler/wiki/Onboarding):

1. `labeler-cache-retention.yml`
2. `labeler-predict-issues.yml`
3. `labeler-predict-pulls.yml`
4. `labeler-promote.yml`
5. `labeler-train.yml`

## Repository Configuration

Across these workflows, the following changes were made to configure the issue labeler for this repository:

1. Set `LABEL_PREFIX` to `"area-"`:
    - `labeler-predict-issues.yml`
    - `labeler-predict-pulls.yml`
    - `labeler-train.yml`
2. Set `DEFAULT_LABEL` to `"needs-area-label"`:
    - `labeler-predict-issues.yml`
    - `labeler-predict-pulls.yml`
3. Remove the `EXCLUDED_AUTHORS` value as we do not bypass labeling for any authors' issues/pulls in this repository:
    - `labeler-predict-issues.yml`
    - `labeler-predict-pulls.yml`
4. Update the pull request labeling branches to include `main` and `release/*`:
    - `labeler-predict-pulls.yml`
5. Remove the `repository` input for training the models against another repository:
    - `labeler-train.yml`
6. Update the cache retention cron schedule to an arbitrary time of day:
    - `labeler-cache-retention.yml`

## Repository Variables

The following [repository variables](../../settings/variables/actions) can be configured to override default workflow behavior:

| Variable | Description | Default |
|----------|-------------|---------|
| `ISSUE_LABELER_PREDICTION_THRESHOLD` | The minimum prediction confidence threshold for issues, used by issue prediction and model testing. | `0.15` |
| `PR_LABELER_PREDICTION_THRESHOLD` | The minimum prediction confidence threshold for pull requests, used by PR prediction and model testing. | `0.13` |

Unset or empty variables use the defaults above. Predictions below the effective threshold apply `DEFAULT_LABEL` instead.

`ISSUE_LABELER_PREDICTION_THRESHOLD` still overrides the issue threshold, but no longer affects pull requests. To override the PR threshold, set `PR_LABELER_PREDICTION_THRESHOLD`.

These are coverage-first starting values informed by the evaluation in [#137023](https://github.com/dotnet/runtime/pull/137023), not previously measured exact cutoffs. The earlier precision and coverage results do not establish results at `0.15` or `0.13`.
