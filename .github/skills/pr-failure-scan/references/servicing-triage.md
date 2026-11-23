# Servicing Failure Interpretation

Use this reference only when the PR targets `release/<major>.<minor>`.

## Relationship to shared KBE search

`pr-failure-scan` and the shared KBE instructions already search
`dotnet/runtime` for:

- open Known Build Errors,
- recent closed Known Build Errors,
- plain area-team trackers,
- open fixes,
- recent merged fixes.

Do not repeat or replace those searches here. Issue scope follows the failure
signature and root cause, not the branch where the issue was first reported.
An issue first filed from main may cover a release failure, and an issue first
filed from a release branch may cover main.

This reference adds branch-aware interpretation after the normal KBE lookup and
draft phase, but before any live publication.

## Branch-aware checks

For each runtime-owned failure:

1. Record the target release branch and selected failure time.
2. Use the tracker and fix evidence from the orchestrator's shared KBE lookup.
   Add draft evidence from `create-kbe` only when that skill was invoked.
3. If an open tracker matches the same root cause, reuse it regardless of its
   first reported branch. Report any matcher gap that prevents the release leg
   from being recognized.
4. The shared KBE instructions normally search for merged fix PRs from the last
   14 days. If a matching KBE is closed as fixed, inspect its linked fix PRs
   regardless of merge date. A fix may have been on main for longer than 14
   days without ever reaching the target release branch.
5. Verify whether the effective fix is present in both main and the target
   release branch. Do not rely only on commit ancestry because servicing fixes
   may be cherry-picked under different commit IDs.
6. If no linked fix is available, perform a targeted merged-PR search using the
   tracker number and stable signature. Do not enumerate unrelated branch
   history.
7. Inspect the applicable `dotnet/dotnet` VMR branch only when the selected
   build, tracker, or fix evidence identifies VMR consumption or codeflow.

Absence from a search result or absence of a recent failing build is not proof
that a branch is unaffected.

## Result fields

Report these independently because more than one can apply:

- `trackerStatus`: shared tracker, closed tracker, no tracker, or unclear
- `matcherStatus`: matched, matcher gap, not applicable, or unclear
- `propagationStatus`: synchronized, backport needed, main fix needed,
  VMR sync gap, cross-branch recurrence, or unclear
- `recommendedAction`: none, correct matcher, request backport, fix main,
  investigate VMR sync, or human review

Before recommending a backport, verify that the fix is a small targeted change
for a security bug, regression, or reliability issue, consistent with the
repository's servicing policy.

For `request backport`, report the verified fix PR, target release branch,
eligibility rationale, evidence that the effective change is absent, and the
suggested maintainer command:

```text
/backport to release/<major>.<minor>
```

Never post the command or invoke the backport workflow.
