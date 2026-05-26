# Shared KBE Analysis Instructions

Use this file when you already have a concrete failure candidate that might need
a `Known Build Error` issue in `dotnet/runtime`.

This file is intentionally limited to the logic shared between:

- the scheduled CI failure scan workflow, and
- local, PR-targeted KBE creation tools.

Consumer-specific behavior stays outside this file. In particular, the caller is
responsible for:

- choosing which failures are in scope,
- deciding whether Build Analysis already recognized a failure as known,
- deciding whether issue creation is live or dry-run,
- collecting any required user confirmations,
- deciding whether skipped outcomes should still produce a draft for human
  review, and
- formatting the final report back to the user.

<a id="shared-kbe-rules"></a>

## Shared rules

1. **One failure shape = one outcome.** Do not create duplicate KBEs for the
   same signature.
2. **One area path per issue.** Title each KBE around a single failure shape
   (assertion text, exception text, or a narrowly-scoped build break), not a
   list of pipelines.
3. **No `Mute` / `Muting` in titles.** Use `Skip`, `Disable`, `Suppress`, or
   `Exclude` when discussing follow-up mitigations.
4. **Do not add `area-*` references to issue titles.** Area triage is handled
   separately.
5. **Do not comment on existing KBEs.** Build Analysis tracks occurrence data in
   the issue body.

<a id="search-existing-kbe"></a>

## Search for an existing KBE

Search open `dotnet/runtime` issues with the `Known Build Error` label. Try
these variations in order, scanning the first ~10 results of each. GitHub
best-match ranking can place noisier hits above the correct one.

1. Full `[FAIL]` line.
2. Assertion text.
3. Exception class + test name.
4. Test class name + `label:"Known Build Error"`, for example
   `SocketBlockingModeTransitionTests label:"Known Build Error"`.
5. Test class name + area label, no KBE filter, for example
   `SocketBlockingModeTransitionTests label:area-System.Net.Sockets`.

Variations 4 and 5 catch sibling failures filed for the same test class on a
different platform or runtime variant, plus pre-existing area-team trackers
that lack the `Known Build Error` label.

If a KBE-labeled search returns a `[Filtered]` marker, treat it as a likely
existing-KBE hit and record
`skipped: integrity-filtered candidate, needs human review` instead of creating
a fresh KBE.

If variation 5 returns a `[Filtered]` marker, record
`linked-tracker: integrity-filtered, needs human review` for cross-linking, but
do not treat it as a KBE substitute.

On any visible hit whose title or body references the same test class on any
platform, record `existing-kbe #<n>` (or `linked-tracker #<n>` for variation 5
when the hit lacks the KBE label) and continue. A hit changes the final action;
it does not end the inspection.

<a id="search-area-team-tracker"></a>

## Search for an area-team tracker

Search for a plain tracker with:

- `is:issue is:open in:title "<test-name>"`
- `in:body "<test-file-path>"`

On hit, record `linked-tracker #<n>`.

A plain tracker is **not** a KBE substitute. Build Analysis only matches
`Known Build Error`-labeled issues with a valid JSON body.

<a id="search-existing-prs"></a>

## Search for existing PRs already handling the failure

### Existing test-disable PR

Search for:

- `is:pr is:open in:title "<test-name>" "[ci-scan]"`
- `is:pr is:open "<test-name>" ActiveIssue`

On hit, record `existing-PR #<n>`.

### In-flight fix PR by anyone

Search broadly, not just `[ci-scan]` PRs:

- `is:pr is:open "<test-name>"`
- `is:pr is:open "<test-file-path>"`
- `is:pr is:open "<assembly>" in:title`

Fetch each candidate body. If it claims to fix this failure or links the same
KBE, record `existing-PR #<n>`.

<a id="verify-embedded-issues"></a>

## Verify every embedded issue number exists

For every `<n>` you plan to embed into source, issue bodies, or PR bodies
(`Linked KBE: #<n>`, `Tracking: dotnet/runtime#<n>`,
`[ActiveIssue("...issues/<n>")]`, inline comments referencing an issue, and so
on), verify that `dotnet/runtime#<n>` is still open before reusing it.

If the issue does not exist or is no longer open, stop and treat it as an
unhandled case that needs human review.

<a id="verify-candidate-kbe-match"></a>

## Verify a candidate KBE actually matches

Before reusing an existing KBE, answer all four questions:

1. Does the candidate KBE describe the same test or test family as the current
   failure?
2. Does its `ErrorMessage` / `ErrorPattern` describe the same failure signature
   (exception class, assertion message, or build-break text)?
3. Is the failing OS in the set the KBE says it impacts?
4. Is the failing architecture in the set the KBE says it impacts?

If any answer is no, do **not** reuse that KBE.

If the candidate KBE is older than ~14 days, it is also reasonable to verify
that Build Analysis still appears to match it. A stale, never-updated body can
be a sign that the signature no longer matches current logs.

<a id="new-kbe-template"></a>

## New-KBE template

Use the literal-substring form by default. Use the regex form only when no
single literal line is specific enough.

<a id="literal-kbe-template"></a>

### KBE issue body - literal substring match

Title:

- `[ci-scan] Test failure: <fully.qualified.TestName>` for test failures
- `[ci-scan] Hang: <fully.qualified.TestName>` for hangs / timeouts
- `[ci-scan] Build break: <short error description>` for compile / link / cmake
  breaks

Labels:

- `Known Build Error`
- `blocking-clean-ci`

````markdown
## Build Information
Build: <link to the relevant dev.azure.com build>
Build error leg or test failing: <AzDO leg name>-<assembly or test name>
Pull request: <link to the PR if this is a PR build, otherwise omit this line>

## Error Details

<!-- Paste the full stack trace or exception output below so readers can understand the failure at a glance.
     This section is for humans — Build Analysis only parses the ## Error Message section. -->

```
<full exception / stack trace excerpt; sanitize as needed>
```

## Error Message

<!-- The JSON blob below is parsed by Build Analysis for automatic matching.
     ErrorMessage is a literal String.Contains substring (case-sensitive, ordinal).
     Set BuildRetry to `true` only for clear infra flakes. ExcludeConsoleLog skips helix log scanning. -->

```json
{
  "ErrorMessage": "<exact substring from the failure log; never a bare test name>",
  "ErrorPattern": "",
  "BuildRetry": false,
  "ExcludeConsoleLog": false
}
```
````

<a id="regex-kbe-template"></a>

### KBE issue body - regex match

Pick only when no single literal line is specific enough. Keep the regex
anchored, prefer `[^\n]*` over `.*`, and avoid catastrophic backtracking.

````markdown
## Build Information
Build: <link>
Build error leg or test failing: <AzDO leg name>-<assembly or test name>
Pull request: <link, omit if not a PR build>

## Error Details

<!-- Same human-readable guidance as the literal template. -->

```
<full exception / stack trace excerpt>
```

## Error Message

<!-- The JSON blob below is parsed by Build Analysis for automatic matching.
     ErrorPattern is a regex with .NET options Singleline | IgnoreCase | NonBacktracking and a 50ms-per-line timeout.
     Set BuildRetry to `true` only for clear infra flakes. ExcludeConsoleLog skips helix log scanning. -->

```json
{
  "ErrorMessage": "",
  "ErrorPattern": "<single-line anchored regex; use `[^\\n]*` instead of `.*`>",
  "BuildRetry": false,
  "ExcludeConsoleLog": false
}
```
````

<a id="kbe-array-form"></a>

### KBE multi-line array form

Use array form when the failure is best described by multiple ordered log lines.
Each element matches one line, in order, with arbitrary lines allowed between
matched elements.

```json
{
  "ErrorMessage": [
    "<test name on one line>",
    "<exception message on a later line>"
  ],
  "ErrorPattern": "",
  "BuildRetry": false,
  "ExcludeConsoleLog": false
}
```

Rules:

- One element = one line. Do not concatenate multiple lines into one element.
- All elements must match in order.
- Do not mix `ErrorMessage` and `ErrorPattern` in one array.
- Do not pad with generic tokens like `exitcode: 139`, `Crash`, or other text
  that does not improve specificity.

<a id="kbe-body-verification"></a>

## KBE body verification

Walk all nine checks before creating a new KBE:

1. The body contains a fenced JSON block.
2. There is exactly one fenced JSON block.
3. The opening fence is exactly three backticks followed by `json` in
   lowercase.
4. The closing fence is exactly three backticks.
5. All four keys are present:
   `ErrorMessage`, `ErrorPattern`, `BuildRetry`, and `ExcludeConsoleLog`.
   Exactly one of `ErrorMessage` / `ErrorPattern` is non-empty; the unused one
   is `""`, not omitted.
6. The signature is **not** a bare identifier. A fully-qualified test name, a
   stack-frame line, or a bare exception type often matches passing or skipped
   lines too.
7. Perform a negative-match smoke test against the failure log. The signature
   must match the failing log, and it must not match `[PASS]` or `[SKIP]` lines.
8. Keep the signature single-line and escape correctly. Build Analysis does not
   strip newlines, ANSI escapes, or time prefixes.
9. JSON escaping is correct. Inside the JSON string value:
   `"` -> `\"`, `\` -> `\\`, real newlines -> `\n`. Regex patterns require
   double escaping for literal backslashes.

<a id="signature-specificity"></a>

## Signature specificity

The `ErrorMessage` / `ErrorPattern` must uniquely identify the failure mode,
not an entire category of crashes.

Reject signatures consisting only of:

- A bare exit code or signal (`exitcode: 139`, `Segmentation fault`, `SIGSEGV`)
- A generic tool + verb (`Crossgen2 failed`, `ilasm failed`, `dotnet build failed`)
- A bare exception type without the message (`BadImageFormatException`, `NullReferenceException`)
- A bare `[FAIL]` line with only the test class name
- A bare fully-qualified test name
- A truncated test-name prefix ending in `_`, `.`, or `*`
- Common infra strings like `Connection reset`, `Operation timed out`, or
  `No space left on device`

Prefer signatures built from, in order:

1. Exact assertion text or exception message
2. Fully-qualified failing test name **and** a specific exception message (use
   array form)
3. A unique native stack frame or symbol
4. A specific JIT method-being-compiled marker plus the stress mode

If you cannot produce a signature meeting this bar, do not create a KBE from
the shared flow. Return the failure as unhandled or human-review-needed instead.

<a id="bad-vs-good-signatures"></a>

## Bad vs good signatures

| Bad | Why bad | Good |
|---|---|---|
| `"Some.Test.Class.TestMethodName"` | bare test name; matches `[PASS]` lines | array: `["Some.Test.Class.TestMethodName", "System.Net.Sockets.SocketException : Try again"]` |
| `"SomeTests.Prefix_"` | trailing `_` / `*` / `.` over-matches | `ErrorPattern: "^SomeTests\\.Prefix_[A-Za-z]+\\b[^\\n]*Xunit\\.Sdk\\."` |
| `"Some.Type.Method"` | matches unrelated stack scans | `ErrorPattern: "^System\\.NullReferenceException\\b[^\\n]*\\n\\s+at Some\\.Type\\.Method\\b"` |
| `"BadImageFormatException"` | bare exception type | `"System.BadImageFormatException: Could not load file or assembly 'System.Private.CoreLib'"` |
| `"Operation timed out"` | matches transient network failures everywhere | array: `["xharness exec android test", "Operation timed out after 3600s"]` with `BuildRetry: false` |

<a id="sanitization"></a>

## Sanitization

When pasting log excerpts into issue or PR bodies, strip:

- JWTs, bearer tokens, `ApplicationGatewayAffinity*=`
- Per-user paths like `/home/<user>/` or `C:\Users\<user>\`
- Machine names from Helix agent strings
- Anything that uniquely identifies a contributor environment

<a id="build-analysis-missed-existing-kbe"></a>

## Existing KBE that Build Analysis likely missed

When a failure appears to match an existing KBE but the caller believes Build
Analysis did not recognize it, capture all of the following in the caller's
final report:

- the matched issue number,
- why the failure still appears to match (same test family, same signature, same
  OS / arch scope, or other concrete evidence),
- what likely prevented recognition, such as:
  - the failure text drifting away from the KBE's `ErrorMessage`,
  - the KBE using an over-narrow OS / architecture scope,
  - the KBE missing one of the required JSON keys,
  - the relevant evidence existing only in a console log while the KBE excludes
    console logs,
  - the failure appearing in an excluded or not-yet-analyzed pipeline.
