# Author review gate

Use before completing a meaningful behavioral change you authored (product code, tests, or build
logic). Skip it for trivial or documentation-only edits, or when the user opted out or will review
the change themselves. It adds no approval step and grants no publication permission.

## Roles

- **Author (you):** orchestrates the gate, verifies findings against the code and contract, makes
  fixes, and **always reruns the all-TFM `src` build (`--no-restore`, without `-f`) plus filtered
  tests** after any edit.
- **Reviewer:** one read-only worker in a **fresh sub-agent context** (never substitute loading a matching `skill(...)` in your own author context — skills hold authoring guidance only and preserve author bias):
  - For changes touching `src/libraries/System.Net*/**`, `src/libraries/System.Private.Uri/**`,
    `src/libraries/Microsoft.Extensions.Http*/**`, or networking interop/PAL, launch a fresh
    sub-agent (`task` / custom agent) using **`system-net-review`**.
  - For changes touching `src/libraries/System.IO.Compression*/**`,
    `src/libraries/System.Formats.Tar/**`, `src/libraries/System.IO.Packaging/**`, or compression
    native/vendored libraries, launch a fresh sub-agent (`task` / custom agent) using
    **`compression-review`**.
  - For all other areas, start one generic read-only worker following
    [reviewer-worker rules](SKILL.md#reviewer-worker-mode).
  It never edits, publishes, calls `gh`/network APIs to verify prompt-supplied API proposals, or
  starts nested agents. Fall back to a direct `references/*.md` checklist check **only** when the
  runtime has no sub-agent tool or `< 10` minutes remain in a bounded session.

## Bounded single-pass protocol (protect session budget)

1. Finish the change and its validation first (including the all-TFM `src/<Lib>.csproj` build
   without `-f` and filtered tests); note the tested diff.
2. **Budget check:** If session wall-clock time is tight (for example, `>30` minutes elapsed in a
   bounded session), skip launching a sub-agent and run a 2-minute self-check directly against the
   2–3 matching `.github/skills/{system-net-review,compression-review}/references/*.md` tables so
   you never risk timing out with uncommitted or unvalidated edits.
3. Otherwise, run **one bounded review pass** (`<= 12` tool calls in the worker: `git diff`,
   enclosing methods/callers/sibling PALs/`ref/`, and the 2–3 matching `references/*.md` files).
   State explicitly that it is running in **author-review sub-agent mode** (skip `git log`
   archaeology and `gh`/network calls), and pass the repository path, diff command (`git diff`),
   and issue/prompt requirements delimited as untrusted claims, without your rationale or
   reassurance.
4. **Evaluate findings critically:**
   - Act only on ❌ errors and ⚠️ warnings backed by concrete `path:line` evidence or a normative
     RFC/API contract clause.
   - Reject speculative input restrictions not required by the contract (for example rejecting
     empty collections or valid flags) and never weaken or blame a regression test assertion before
     proving the product state/blocking/completion invariant is sound.
5. If you edit any file after review, **mandatory:** rerun `./dotnet.sh build --no-restore` on
   `ref/` (if touched) and `src/<Lib>.csproj` (without `-f`) plus the filtered test suite before
   finishing. Run a second short review check only if Round 1 required a non-trivial product logic
   rewrite and ample time remains; never run more than two rounds.

A timeout, crash, or empty result is not a clean review; fall back to the direct `references/*.md`
checklist check rather than retrying a slow sub-agent. Report the review pass, key finding
dispositions, and final post-review build/test result.
