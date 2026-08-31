---
name: pr-failure-scan
description: >
  Analyze a dotnet/runtime PR's CI failures, skip failures already known to
  Build Analysis, find matching Known Build Errors, and create or draft new
  KBEs for the remaining failures. Supports dry-run output to local markdown
  files instead of creating GitHub issues.
---

# PR Failure Scan Skill

Use this skill for requests like:

- `/pr-failure-scan <PR URL>`
- `/pr-failure-scan <PR URL> dryrun`
- `create KBEs for PR <number>`

This skill is **PR-targeted** and **local-account-aware**. It complements the
scheduled CI failure scan workflow but does not try to reproduce the workflow's
safe-output or follow-up PR behavior.

## Shared instructions

Before triaging any failure candidate, read and follow:

- `.github/workflows/shared/create-kbe.instructions.md`

That file owns the shared KBE lookup flow, KBE body template, signature
specificity guidance, verification checklist, and the rules for identifying
"existing KBE that Build Analysis likely missed".

## Step 0: Parse input

Accept one of:

- a PR number,
- a PR URL,
- text containing a PR reference,
- an optional trailing `dryrun` token.

Interpret `dryrun` case-insensitively. The source repository is always
`dotnet/runtime`.

Examples:

- `/pr-failure-scan 123456`
- `/pr-failure-scan https://github.com/dotnet/runtime/pull/123456`
- `/pr-failure-scan https://github.com/dotnet/runtime/pull/123456 dryrun`

## Step 1: Resolve identities and require explicit permission for non-owned PRs

1. Resolve the target PR metadata: title, author login, URL, head SHA, base
   branch, state, draft status.
2. Resolve the currently authenticated GitHub user (`gh api user` is fine).
3. Compare the PR author's login with the authenticated user's login.

If they differ, you **must** ask for explicit confirmation before doing the rest
of the run. Running the command itself is **not** enough permission.

The confirmation must warn the user that this skill can create GitHub issues
under their account, and that analyzing an unknown PR carries risk because the
resulting issue content and decisions will be attributed to them.

If the user declines, stop immediately.

## Step 2: Gather PR CI context

Use the latest completed check runs for the PR head SHA.

Collect all of:

1. The `Build Analysis` check run payload from the GitHub REST API.
2. The non-success CI check runs for the PR head SHA.
3. The AzDO build URLs linked from those check runs.

The local skill has a different candidate-source model than the scheduled
workflow:

- The workflow starts from a fixed main-branch pipeline list.
- This skill starts from one PR and must use the PR's Build Analysis results to
  decide what is already known, what is still unknown, and which failed
  pipelines were excluded from Build Analysis.

## Step 3: Decide which failures are in scope

Build the candidate list from two sources:

1. **Build Analysis unknowns on analyzed pipelines.**
   - Parse the `Build Analysis` check text for `Create issue in this repo` links
     and the surrounding failure description.
   - Each such entry is an in-scope candidate unless you later find an existing
     matching KBE.
2. **Failed pipelines explicitly excluded from Build Analysis.**
   - Parse the Build Analysis warning section listing pipelines excluded from
     analysis.
   - For each excluded pipeline that also failed on this PR, inspect its AzDO
     build/timeline/logs and derive concrete failure candidates yourself.

Skip any failure that Build Analysis already treated as known. The skill should
not re-triage failures already recognized by Build Analysis.

If Build Analysis and the raw PR checks disagree, prefer the raw CI evidence and
report the disagreement in the final output.

## Step 4: Analyze each candidate failure

For each candidate failure, do the following:

1. Gather the most concrete available evidence:
   - the Build Analysis excerpt or create-issue link context,
   - the AzDO failed timeline record,
   - the relevant build or task log,
   - for Helix-submitted legs, the Helix console log or failure details when
     accessible.
2. Extract the narrowest signature that fits the shared rules.
3. Run the shared KBE lookup flow from
   `.github/workflows/shared/create-kbe.instructions.md`.
4. Classify the candidate into exactly one of these buckets:
   - **new KBE needed**
   - **existing KBE matched but Build Analysis likely missed it**
   - **unhandled**

### PR-specific guidance for excluded or partially-analyzed pipelines

For PR-targeted runs, it is acceptable to prepare a **draft-only** KBE candidate
for a Build Analysis unknown or excluded failure even when the evidence is not
yet strong enough for fully automated filing, as long as you clearly mark why
the case still needs human review.

Use this only for the local skill. Do **not** feed this relaxed draft behavior
back into the scheduled workflow rules.

Typical cases where a draft-only candidate may still be useful:

- Build Analysis already surfaced a `Create issue in this repo` link for the
  failure.
- The pipeline is excluded from Build Analysis, but the PR clearly contains a
  repeated failure family worth turning into a reviewable draft.
- The failure has a recognizable issue shape, but the exact log line still
  needs to be copied from a deeper log.

For such draft-only cases:

- keep the issue in the **new KBE needed** bucket only for this local skill,
- make the uncertainty explicit in the draft body,
- explain the missing evidence in the final report.

If a candidate is too weak even for a useful draft, place it in **unhandled**
with a concrete reason.

### Dry-run drafting guidance

When producing a dry-run draft, optimize for a human-reviewable issue proposal
that is close to what a developer would want to file from the PR, even if the
draft still needs final cleanup before live filing.

In particular:

- Prefer a descriptive draft title that names the concrete failure shape over a
  generic `Hang:` / `Test failure:` title when that better communicates the
  issue for a human reviewer.
- If the failure happens during xUnit discovery, prefer the discovery-time
  signature over a later native-crash bucket when both are visible and the
  discovery signature is the more stable reusable match.
- For infra-shaped submission failures, prefer a multi-token array signature
  over one long literal line when the issue is really identified by a small set
  of stable tokens.

## Step 5: Write draft files and optionally create live issues

### Draft files for all new KBEs

For every item in the **new KBE needed** bucket, always write a markdown draft
file before deciding whether to create a GitHub issue. This applies to both
`dryrun` and live mode.

1. Write one markdown file per `new KBE needed` item.
2. Prefer the session artifact directory if it is available. Otherwise, use a
   temporary directory outside the repo. Do not write draft files into the repo
   checkout.
3. Report the exact file path for every draft.

Each draft file should contain:

- proposed title,
- proposed labels,
- a human-review-oriented draft body,
- any explicit note when the draft still needs more evidence before live filing.

These dry-run files are **review artifacts**, not necessarily byte-for-byte live
issue bodies. Keep them easy to compare and edit on disk.

Prefer this layout for dry-run files:

````markdown
# Draft KBE issue

- Proposed title: `...`
- Proposed labels: `Known Build Error`, `blocking-clean-ci`

## Draft issue body

## Build Information
Build: ...
Build error leg or test failing: ...

## Error Message

```json
...
```

**Failure details:**
```text
...
```

**Affected legs:**
- ...

**Console Log:**
- ...

**First build in window:**
- ...

**Recommended action:**
...
````

For dry-run artifacts, prefer the layout above over adding extra sections like
`Pull request:` or `## Error Details` unless they are truly necessary for
understanding the draft.

When linking to a draft file in a user-facing message or confirmation question,
include both:

- a clickable local-file link when the UI supports one, and
- the plain absolute path.

### Dry-run mode

In `dryrun` mode:

1. Do **not** create any GitHub issues.
2. Stop after writing the draft files and producing the final report.

### Live mode

In live mode:

1. First prepare the complete batch of proposed issues.
2. Write all draft files as described above.
3. Process the proposed issues one by one. For each KBE:
   - Ask the user whether to create this specific issue.
   - The question must include:
     - the proposed issue title,
     - a short description of what the KBE is about,
     - the list of failures it covers, or a shortened summary if the list is
       long,
     - a clickable draft-file link when possible plus the plain absolute path,
     - a clear statement that the GitHub issue will be created on behalf of the
       currently authenticated user.
   - Only create that issue if the user explicitly confirms.
   - If the user declines, skip that issue and continue to the next proposed
     KBE.

Use the shared KBE template guidance for the body.

Every live-created issue body must include a visible AI disclosure note because
the issue is being filed under a developer account. A concise note is enough,
for example:

> [!NOTE]
> This issue draft was prepared with GitHub Copilot assistance and reviewed by
> the submitting developer.

## Step 6: Final output format

The final response must contain all four sections below.

### 1. New issues created or draft files

List either:

- the created issue URLs, or
- the dry-run draft file paths.

### 2. New issues and the failures they cover

For every newly created or newly drafted KBE, provide:

- title,
- URL or file path,
- short description of what the issue is about,
- the specific failure or failure family it covers.

### 3. Existing KBE matches that Build Analysis likely missed

For every such failure, provide:

- failing leg / build context,
- matched KBE number and URL,
- why it appears to match,
- why Build Analysis likely missed it.

### 4. Failures not handled

For every unhandled failure, provide:

- failing leg / build context,
- why it was not handled,
- what evidence or follow-up would be needed to handle it.
