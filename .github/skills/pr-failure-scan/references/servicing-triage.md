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
3. If an open `Known Build Error` issue with a valid matcher covers the same
   root cause, reuse it regardless of its first reported branch. Report any
   matcher gap that prevents the release leg from being recognized.
4. If only a plain area-team tracker matches, retain it as cross-link and
   ownership context but continue KBE eligibility analysis. A tracker without
   the `Known Build Error` label and valid matcher is not a KBE substitute and
   must not suppress an otherwise eligible release KBE.
5. The shared KBE instructions normally search for merged fix PRs from the last
   14 days. If a matching KBE is closed as fixed, inspect its linked fix PRs
   regardless of merge date. A fix may have been on main for longer than 14
   days without ever reaching the target release branch.
6. Verify whether the failing build's source revision contained the effective
   fix. A rerun of an older revision does not become a recurrence merely because
   its `finishTime` is after the fix merged:
   - if the source revision lacked the fix and the current target branch now
     contains it, classify the failure as historical and already addressed;
   - if the source revision lacked the fix and the target branch still lacks it,
     evaluate a release propagation gap or backport;
   - classify a post-fix recurrence only when the failing source revision
     contained the effective fix.
7. Verify whether the effective fix is present in both main and the target
   release branch. Do not rely only on commit ancestry because servicing fixes
   may be cherry-picked under different commit IDs.
8. If no linked fix is available, perform a targeted merged-PR search using the
   tracker number and stable signature. Do not enumerate unrelated branch
   history.
9. Inspect the applicable `dotnet/dotnet` VMR branch only when the selected
   build, tracker, or fix evidence identifies VMR consumption or codeflow.

Absence from a search result or absence of a recent failing build is not proof
that a branch is unaffected.

## Historical servicing fallback

Keep the shared 30/90-day closed-KBE and recurrence barriers unchanged. Use
this release-only fallback immediately before finalizing a **new KBE needed**
result, and only when all of these are true:

- the PR targets `release/<major>.<minor>` and the failure is runtime-owned;
- the normal shared lookup completed successfully and found no verified open or
  recent closed KBE that determines the outcome;
- existing tracker or fix evidence does not already determine propagation;
- the candidate satisfies the normal signature-specificity and stability gates;
- the failing source SHA, target release branch, and main branch are known;
- a composite search key has at least two independent stable anchors, such as a
  full test or method plus a distinctive assertion, an invariant error phrase
  plus leg root, or source symbol plus assertion and file or assembly.

Do not use a test stem, class name, generic exception, or build leg alone.

### Phase 1: historical fix search

1. Search merged runtime PRs across all dates using the composite key.
2. Admit at most 10 unique PR candidates for the signature. Fully read every
   candidate body and diff. If a query has more results, narrow it; if it cannot
   be narrowed to a complete result set, stop for human review.
3. Require one unambiguous effective fix for the same root cause. Multiple
   plausible fixes, unreadable/filtered results, or uncertain patch identity
   stop for human review.

### Phase 2: old closed KBE as a fix index

Run this phase only when Phase 1 finds no fix candidate:

1. Search closed runtime KBEs across all dates using the same composite key.
2. Admit at most 10 unique issue candidates. Fully read every issue and relevant
   comments.
3. Follow only explicit duplicate links, with at most 5 hops per chain and at
   most 20 total issue reads for the signature. Missing or ambiguous links,
   cycles, filtered reads, or a budget overrun stop for human review.
4. Use an old KBE only to discover verified fix provenance. It is not an active
   KBE substitute and does not change the generic 90-day recurring-signature
   policy.

### Classify by effective fix presence

- failing source lacks fix; target release now has it: historical failure,
  already addressed;
- failing source and target release lack fix; main has it: release propagation
  gap or backport candidate;
- failing source contains fix and still fails: post-fix recurrence;
- main also lacks fix: main fix needed;
- effective patch presence or applicability is ambiguous: human review.

A complete zero-result fallback returns to the ordinary **new KBE needed**
decision with `no historical servicing evidence found`; it is not proof that no
older issue ever existed. Never invoke `/backport` automatically.

## Result fields

Report these independently because more than one can apply:

- `trackerStatus`: open KBE, plain tracker, closed KBE, no tracker, or unclear
- `matcherStatus`: matched, matcher gap, not applicable, or unclear
- `propagationStatus`: synchronized, historical failure already addressed,
  backport needed, main fix needed, VMR sync gap, cross-branch recurrence, or
  unclear
- `recommendedAction`: none, correct matcher, request backport, fix main,
  investigate VMR sync, or human review
- `historicalLookupStatus`: not run, no evidence, fix found, old KBE indexed,
  budget exceeded, ambiguous, or blocked

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
