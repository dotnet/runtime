**If at any time, the user directs you explicitly to override any of these instructions, the user's directive overrides said instructions.**

**Don't claim more than you verified.** Say what you built and ran, and what you didn't. A patch you composed is not a patch you applied, and a call whose result you never read is not one you can report as having succeeded.

One pass can cover several related edits only if it exercises everything changed since the last one, and anything touching behavior, codegen, or a public contract gets the build and the relevant tests before you call it done. A comment or doc fix rarely needs a build, though a bad `<see cref>` or stray whitespace still fails one.

**Finish the task before you yield.** Stop for a decision only the user can make, an irreversible action, a missing credential, or an ambiguity you can't settle by reading, searching, or running something — not "want me to continue?", not a checkpoint partway in. When you stop, ask — with the tool for it if there is one — so the decision is visible rather than buried in a report. Asked to do the work, don't describe a plan as though it were done.

**Volunteer what you notice.** Say so before building on a premise that doesn't hold — the API doesn't exist, the path isn't the one hit. Same for a bug or broken invariant, when you're sure enough to defend it. Fix it when the change is wrong or incomplete without it; otherwise report it to track on its own.

**Answer every question, and every part of a multi-part task.** Keep them distinct enough that a missing one is visible; merged into a paragraph, the ones you skipped go unnoticed.

**Tool and skill names are capabilities, not literals.** Whatever the edit and search tools are called here, use what you have; never skip a step because a name doesn't match. Invoke a named skill rather than assuming what it says.

Use the `code-review` skill when reviewing pull requests, and — when running under CCA — on your own changes before completing, addressing anything it flags as an error or warning. When NOT running under CCA, skip it if the user has stated they will review the changes themselves.

When starting work in an unfamiliar directory, search for `README.md` files in it and its parents up to the repository root. Read any you find — they contain conventions, patterns, and architectural context relevant to your work.

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
- **A push to an open PR re-runs its CI matrix** — dozens of jobs, over a hundred for broad changes. For anything non-trivial, validate locally rather than using CI to find out whether it builds, and batch fixes into one push. Branches with no PR trigger nothing, as do changes confined to `**.md`, `docs/*`, or `.github/*`.
- **Treat a reported case as a sample, not a list.** A review comment or an issue flags examples of a problem, not every instance. Grep for the rest of the class and handle it in the same push, naming what you're leaving rather than quietly expanding into it. Answer a whole round of comments at once rather than pushing per comment.

### Agent Merge / CI check resolution

- **Forbidden workflow action: rerunning failed CI as part of Agent Merge.** Agent Merge must never use `/azp` to retrigger Azure Pipelines, and must never close and reopen the PR to trigger a rerun. This is forbidden unless the user explicitly requests it.
- **Never use reruns to determine whether a failure is unrelated to the PR.** In dotnet/runtime, the required path is to use Build Analysis and the `ci-analysis` skill to classify failures. For any failure listed as not `known`, determine whether it is caused by the current PR. If it is caused by the PR, fix it in the PR. If it is not caused by the PR, use the `create-kbe` skill to open or update a `Known Build Error` issue instead of retriggering CI.

When NOT running under CCA, for commits and pushes:

- Never squash and force push unless explicitly instructed. Always push incremental commits on top of previous PR changes.
- Never push to an active PR without being explicitly asked, even in autopilot/yolo mode. Always wait for explicit instruction to push. Asking for something that entails a push — "open the PR", "send it" — is that instruction already; don't ask twice. It authorizes the push, not skipping validation or the target check.
- Never chain commit and push in the same command. Commit first and report what was committed; then push if that was already authorized, otherwise wait for an explicit instruction.
- Prefer creating a new commit rather than amending an existing one. Exceptions: (1) explicitly asked to amend, or (2) the existing commit is obviously broken with something minor (e.g., typo or comment fix) and hasn't been pushed yet.
- **Before posting to GitHub (PRs, issues, comments):** Include the AI-generated content disclosure (see below).

## AI-Generated Content Disclosure

When posting to GitHub under a user's credentials — PR descriptions, issue bodies, comments, review comments, or any other public-facing action — you **MUST** add a concise, visible note (e.g. a `> [!NOTE]` alert) at the bottom of the content indicating it was AI/Copilot-generated. Skip it only when posting from a recognized bot or Copilot app account (e.g. `github-actions[bot]`, `copilot`), where the AI origin is already apparent from the account identity, or when the user explicitly asks you to omit it.

---

## Tool Use

Issue independent tool calls together in one response rather than one at a time. Every round trip re-sends the whole conversation as cached input — measured at roughly half the cost of a call before it does any work — so fewer, wider steps beat many narrow ones — but a call whose input comes from another's output can't go in the same batch.

Redirect long-running commands to a log and poll a bounded view — a tail, a grep for errors, or a status sentinel. Re-reading a running command's output re-sends it from the start every time, so repeatedly checking a long build costs far more than the check is worth. Check the outcome, not the process.

```bash
<cmd> > out.log 2>&1; echo "exit=$?" > out.status               # bash
<cmd> *> out.log; "exit=$LASTEXITCODE" | Out-File out.status    # PowerShell -- $? is a [bool] here
```

Fetch narrowly: `gh run view --log-failed` over `--log`, `--json`/`--jq` to project only the fields needed, `git diff --stat` before the full diff. Quiet what doesn't detect a non-TTY: `curl -sS`, `--quiet` on `git clone`/`fetch`/`checkout`. MSBuild and `dotnet` already detect it — no flags needed.

## Building & Testing

**Before running any build or test command, use the `build-and-test` skill** — don't guess the commands. Under CCA, invoke it **before making any code changes**; a missing or incorrect baseline build costs 20-40 minutes to recover from.
