# API Approval Verification

When Step 0 detects that a PR introduces new public API surface (changes to `ref/` assembly source files or new `public` members in `src/` files), execute this procedure during Step 2. This is a **blocking** procedure — if any step fails, the review verdict must reflect it as ❌.

## 1. Locate the `api-approved` Issue

Parse the PR description for issue references (`Fixes #N`, `Closes #N`, `Resolves #N`, or bare `#N` references). Use the GitHub API (via `gh` CLI or GitHub MCP tools) to check whether any linked issue has the `api-approved` label.

- **If no `api-approved` issue is found**: Report as ❌ error — "This PR adds new public API surface but has no linked issue with the `api-approved` label. New public APIs require an approved proposal before PR submission. Either link the approved issue or mark the new APIs as `internal` pending API review."

## 2. Extract the Approved API Shape

The final approved API shape is posted as a **comment** by the **same user who applied the `api-approved` label**, at the time the label was applied. This comment is the single source of truth for the approved API.

1. Fetch the issue's timeline or events to identify which user applied the `api-approved` label.
2. Fetch the issue comments and find the comment posted by that same user at approximately the same time as the label was applied.
3. The approved API will be in a fenced code block (` ```csharp ` or ` ```diff `) within that comment.

- **If the approved API comment cannot be found**: Report as ❌ error — "Cannot locate the approved API shape from the `api-approved` issue. The approved API must be posted as a comment by the reviewer who applied the `api-approved` label."

## 3. Compare Implementation Against Approved Shape

Compare the PR's `ref/` assembly source changes against the approved API shape. Check:

- **Namespaces and type names** — must match exactly.
- **Method signatures** — name, return type, and parameter types must match.
- **Parameter names** — a mismatch is a source breaking change (affects named arguments and late-bound scenarios).
- **Property names and types** — must match.
- **Extra public surface** — any public API in the implementation that is not in the approved shape is unapproved and must be flagged.
- **Missing approved surface** — any API in the approved shape that is not in the implementation should be flagged (it may be intentionally deferred, but the reviewer must be made aware).

Flag discrepancies with the following severity:

- ❌ **error**: Extra unapproved public API, wrong parameter names, wrong method signatures, wrong type names.
- ❌ **error**: Missing approved API (unless the PR description explicitly states partial implementation with a tracking issue for the remainder).
- ⚠️ **warning**: Minor differences that may be intentional but warrant confirmation (e.g., additional `[EditorBrowsable(Never)]` attributes, extra `#if` guards for TFM compatibility).
