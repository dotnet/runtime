**Any code you commit MUST compile, and new and existing tests related to the change MUST pass.**

You MUST make your best effort to ensure any code changes satisfy those criteria before committing. Build and run the relevant tests after your last edit — do not assume a change fixed a failure you saw, actually run them again to confirm. If for any reason you were unable to build or test code changes, you MUST report that. You MUST NOT claim success unless all builds and tests pass as described above.

Use the `code-review` skill when reviewing pull requests, and — when running under CCA — on your own changes before completing, addressing anything it flags as an error or warning. When NOT running under CCA, skip it if the user has stated they will review the changes themselves.

Before making changes to a directory, search for `README.md` files in that directory and its parent directories up to the repository root. Read any you find — they contain conventions, patterns, and architectural context relevant to your work.

If the changes are intended to improve performance, or if they could negatively impact performance, use the `performance-benchmark` skill to validate the impact before completing.

When writing or reviewing SIMD / hardware-intrinsics code (anything using `Vector128`/`Vector256`/`Vector512`, `Vector<T>`, or the platform intrinsics in `System.Runtime.Intrinsics.*`), use the `vectorization` skill.

You MUST follow all code-formatting and naming conventions defined in [`.editorconfig`](/.editorconfig).

In addition to the rules enforced by `.editorconfig`, when writing C# you SHOULD:

- Prefer file-scoped namespace declarations and single-line using directives.
- Ensure that the final return statement of a method is on its own line.
- Use pattern matching and switch expressions wherever possible.
- Use `nameof` instead of string literals when referring to member names.
- Always use `is null` or `is not null` instead of `== null` or `!= null`.
- Trust the C# null annotations and don't add null checks when the type system says a value cannot be null.
- Prefer `?.` if applicable (e.g. `scope?.Dispose()`).
- Use `ObjectDisposedException.ThrowIf` where applicable.
- If you add new code files, ensure they are listed in the csproj file (if other files in that folder are listed there) so they build.
- When adding XML documentation to APIs, follow the guidelines at [`docs.prompt.md`](/.github/prompts/docs.prompt.md).

When writing or modifying tests, you SHOULD:

- Strongly prefer to add new unit tests to existing test code files rather than creating new code files.
- When adding new test files, examine the directory structure of sibling tests first. Some test directories use flat files (e.g., `GCEvents.cs` alongside `GCEvents.csproj`) while others use per-test subdirectories. Match the existing convention.
- Avoid adding a regression comment citing a GitHub issue or PR number unless explicitly asked to include such information.
- Prefer using `[Theory]` with multiple data sources (like `[InlineData]` or `[MemberData]`) over multiple duplicative `[Fact]` methods. Fewer test methods that validate more inputs are better than many similar test methods.
- When running tests, if possible use filters and check test run counts, or look at test logs, to ensure they actually ran.
- Do not finish work with any tests commented out or disabled that were not previously commented out or disabled.
- Do not emit "Act", "Arrange" or "Assert" comments.

For markdown (`.md`) files, ensure there is no trailing whitespace at the end of any line.

## Pull Requests

- **One concern per PR.** Split large or mixed changes. Do large refactorings and mechanical renames in their own PR, separate from logic changes.
- **New public API requires an approved proposal before submission** — PRs adding unapproved API will be closed. Use the `api-proposal` skill; until approval lands the API stays `internal` in any submitted PR. A proposal's prototype branch is exempt and keeps its surface public — it's evidence, not a submission.
- **Core component changes should start with an issue.** Changes to the host, VM, or JIT need a GitHub issue describing the problem and motivation first.
- **Put the measurements in the description** for performance changes — BenchmarkDotNet results, or codegen and instruction-count evidence for low-level work.
- **Behavioral changes need breaking-change documentation**, even prerelease-to-prerelease. Use the `breaking-change-doc` skill.
- **Merge to main first, then `/backport`.** Servicing backports are limited to security bugs, regressions, and reliability issues, and should be small targeted fixes rather than refactorings.

When NOT running under CCA, for commits and pushes:

- Never squash and force push unless explicitly instructed. Always push incremental commits on top of previous PR changes.
- Never push to an active PR without being explicitly asked, even in autopilot/yolo mode. Always wait for explicit instruction to push.
- Never chain commit and push in the same command. Always commit first, report what was committed, then wait for an explicit push instruction. This creates a mandatory decision point.
- Prefer creating a new commit rather than amending an existing one. Exceptions: (1) explicitly asked to amend, or (2) the existing commit is obviously broken with something minor (e.g., typo or comment fix) and hasn't been pushed yet.
- **Before posting to GitHub (PRs, issues, comments):** Include the AI-generated content disclosure (see below).

## AI-Generated Content Disclosure

When posting to GitHub under a user's credentials — PR descriptions, issue bodies, comments, review comments, or any other public-facing action — you **MUST** add a concise, visible note (e.g. a `> [!NOTE]` alert) at the bottom of the content indicating it was AI/Copilot-generated. Skip it only when posting from a recognized bot or Copilot app account (e.g. `github-actions[bot]`, `copilot`), where the AI origin is already apparent from the account identity, or when the user explicitly asks you to omit it.

---

## Building & Testing

**Before running any build or test command, use the `build-and-test` skill** — don't guess the commands. Under CCA, invoke it **before making any code changes**; a missing or incorrect baseline build costs 20-40 minutes to recover from.
