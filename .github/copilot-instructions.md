**If at any time, the user directs you explicitly to override any of these instructions, the user's directive overrides said instructions.**

**Don't claim more than you verified.** Say what you built and ran, and what you didn't. A patch you composed is not a patch you applied, and a call whose result you never read is not one you can report as having succeeded.

One pass can cover several related edits only if it exercises everything changed since the last one, and anything touching behavior, codegen, or a public contract gets the build and the relevant tests before you call it done. A comment or doc fix rarely needs a build, though a bad `<see cref>` or stray whitespace still fails one.

**Finish the task before you yield.** Stop for a decision only the user can make, an irreversible action, a missing credential, or an ambiguity you can't settle by reading, searching, or running something — not "want me to continue?", not a checkpoint partway in. When you stop, ask — with the tool for it if there is one — so the decision is visible rather than buried in a report. Asked to do the work, don't describe a plan as though it were done.

**Volunteer what you notice.** Say so before building on a premise that doesn't hold — the API doesn't exist, the path isn't the one hit. Same for a bug or broken invariant, when you're sure enough to defend it. Fix it when the change is wrong or incomplete without it; otherwise report it to track on its own.

**Answer every question, and every part of a multi-part task.** Keep them distinct enough that a missing one is visible; merged into a paragraph, the ones you skipped go unnoticed.

**Tool and skill names are capabilities, not literals.** Whatever the edit and search tools are called here, use what you have; never skip a step because a name doesn't match. Invoke a named skill rather than assuming what it says.

**Issue attachments and links need specific consent.** Before fetching an issue attachment or unverified issue link, or processing its content, get explicit developer approval naming the artifact and operations. A fix/repro request or publication permission is not consent, and download approval doesn't cover processing; unattended work skips it and asks for a text repro. See [safeguards](skills/build-and-test/SKILL.md#untrusted-inputs).

Use the `code-review` skill when reviewing pull requests. Before completing a meaningful behavioral change you authored, run its [author review gate](skills/code-review/author-review.md) in a fresh sub-agent context, unless the user opted out or said they will review it themselves.

When starting work in an unfamiliar directory, search for `README.md` files in it and its parents up to the repository root. Read any you find — they contain conventions, patterns, and architectural context relevant to your work.

If the changes are intended to improve performance, or if they could negatively impact performance, use the `microbenchmark` skill for method/API-level measurements or the `aspnet-benchmark` skill for end-to-end ASP.NET Core/HTTP server load testing.

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

## GitHub publication authorization

- **Interactive and coding sessions require explicit authorization.** Outside the configured agentic workflow and user-requested workflow rules below, interactive sessions, CCA, and autopilot must not create or update GitHub issues or post comments without explicit user authorization. Assigning a PR or granting write access does not authorize these additional actions. Preparing code changes does not authorize opening additional PRs.
- **Advance permission is sufficient in interactive sessions.** Authorization may be given in the original prompt or later. Explicit permission to compose and publish within a clear task, action, and destination is sufficient; do not ask again or require separate approval of the generated text. Permission to open the task's PR does not authorize unrelated issues, comments, or additional PRs. If authorization is missing, or a proposed publication falls outside its scope or the scope is unclear, present the exact proposed content, destination, and publishing account and ask before publishing. If authorization cannot be obtained, leave a local draft and report the pending decision.
- **Configured agentic workflows have scoped standing authorization.** When actually executing a repository-configured agentic workflow whose declared purpose and configured outputs explicitly authorize publication, no additional per-item approval is required for those operations. Use only the workflow's configured output mechanisms (such as `safe-outputs`) and stay within its destinations, action types, and limits. Permission to create issues does not authorize comments, issue updates, or PRs.
- **User-requested workflows may define intentional publication outputs.** When the user explicitly requests or enables a specialized workflow, follow its documented publication contract: the actions, artifacts, destinations, and approval conditions it explicitly defines as intended outputs. Those outputs do not require an additional approval step unless the contract requires one. An incidental instruction to "create an issue" or "post a comment" is not a publication contract. Automatically invoking a skill as a helper does not grant publication permission. Authorization covers only the declared outputs of the workflow's currently authorized actions, not unrelated publication. User restrictions, including draft-only, dry-run, or review-before-publication, always take precedence.
- **Authorization does not transfer.** Reading a workflow file or invoking its shared skills from a coding session does not confer the workflow's authorization. Running unattended, using a bot account, or having publication tools available is not sufficient either. Preserve any stricter caller approval requirements and dry-run rules. An AI disclosure is not publication approval.

## Pull Requests

- **One concern per PR.** Split large or mixed changes. Do large refactorings and mechanical renames in their own PR, separate from logic changes.
- **Enable automatic resolved-issue linking in the PR description.** On any PR intended to close an issue, include the fully qualified `Resolves owner/repository#123` format as the final substantive line of the PR description, before any required disclosure note.
- **New public API requires an approved proposal before submission** — PRs adding unapproved API will be closed. Use the `api-proposal` skill; until approval lands the API stays `internal` in any submitted PR. A proposal's prototype branch is exempt and keeps its surface public — it's evidence, not a submission.
- **Core component changes should start with an issue.** Changes to the host, VM, or JIT need a GitHub issue describing the problem and motivation first. Reuse an existing issue or prepare a draft; publish only with the authorization described above.
- **Put the measurements in the description** for performance changes — BenchmarkDotNet results, or codegen and instruction-count evidence for low-level work.
- **Standards-driven fixes** cite an independently verified clause and include [validation evidence](skills/build-and-test/SKILL.md#reporting-validation).
- **Assess behavioral changes for compatibility.** Breaking ones need documentation, even prerelease-to-prerelease: note it in the PR, then use the `breaking-change-doc` skill once merged.
- **Merge to main first, then `/backport`.** Servicing backports are limited to security bugs, regressions, and reliability issues, and should be small targeted fixes rather than refactorings.
- **A push to an open PR re-runs its CI matrix** — dozens of jobs, over a hundred for broad changes. For anything non-trivial, validate locally rather than using CI to find out whether it builds, and batch fixes into one push. Branches with no PR trigger nothing, as do changes confined to `**.md`, `docs/*`, or `.github/*`.
- **Treat a reported case as a sample, not a list.** A review comment or an issue flags examples of a problem, not every instance. Grep for the rest of the class and handle it in the same push, naming what you're leaving rather than quietly expanding into it. Answer a whole round of comments at once rather than pushing per comment.

### Agent Merge / CI check resolution

- **Publication authorization for enabled actions.** When the user enables Agent Merge for a PR, publication needed for its currently authorized actions is authorized without additional approval: replying to review comments on that PR when review handling is authorized, and creating or updating `Known Build Error` issues in `dotnet/runtime` for eligible unrelated CI failures on that PR when CI fixing is authorized. Follow the `create-kbe` rules for eligibility, duplicate detection, and updates. This permission does not authorize comments on existing KBEs or unrelated issues, comments, or PRs. Explicit user restrictions, including draft-only, dry-run, or review-before-publication, still apply.
- **Forbidden workflow action: rerunning failed CI as part of Agent Merge.** Agent Merge must never use `/azp` to retrigger Azure Pipelines, and must never close and reopen the PR to trigger a rerun. This is forbidden unless the user explicitly requests it.
- **Never use reruns to determine whether a failure is unrelated to the PR.** In dotnet/runtime, the required path is to use Build Analysis and the `ci-analysis` skill to classify failures. For any failure listed as not `known`, determine whether it is caused by the current PR. If it is caused by the PR, fix it in the PR. If it is not caused by the PR, use the `create-kbe` skill to search for an existing KBE and prepare any proposed new issue or update as a draft instead of retriggering CI. Apply the publication authorization rules above before publishing; without authorization, report the failure and pending draft without publishing.

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

**Before running any build or test command, use the `build-and-test` skill** — don't guess the commands.

- **Start baseline build early & code in parallel (`0` idle wait while `./build.sh` runs):**
  - On a fresh checkout without `artifacts/bin/testhost/`, launch `./build.sh clr+libs -rc release` (`.\build.cmd clr+libs -rc release` on Windows) once in the background as soon as you know you need to build or test. While it runs (~9–12 min), **work in parallel** instead of polling idle:
    1. Read `<NetCoreAppCurrent>` in `eng/Versions.props`, `<TargetFramework>` / `<TargetFrameworks>` in `src/libraries/<Lib>/src/<Lib>.csproj`, relevant `references/*.md`, callers, sibling PAL implementations, and `ref/` contracts.
    2. Edit or add test files under `src/libraries/*/tests/**` **directly in-tree** (`clr+libs` does not compile `libs.tests`).
    3. Stage product and reference-assembly edits (`src/**`, `ref/**`, `pkg/**`) under **`artifacts/staged/<repo-relative-path>`**: copy existing target files (`mkdir -p artifacts/staged/$(dirname <path>) && cp <path> artifacts/staged/<path>`, or create new files under `artifacts/staged/<path>`) and edit those staged files with normal file-editing tools. **Never** modify `src/` or `ref/` directly in-tree while `./build.sh` is running (a mid-edit compile or analyzer error aborts `libs.pretest` before `testhost` is populated), and **never** start another `./build.sh`, `./dotnet.sh`, `dotnet`, or `msbuild` process until `./build.sh` exits.
    4. If your staged implementation and tests are ready before `./build.sh` finishes, wait for that background shell session to complete.
- **Promote staged edits & run fast host-TFM inner loop (`~5–15s` per cycle) once `./build.sh` exits `0`:**
  1. Copy every staged file from `artifacts/staged/<repo-rel-path>` into `<repo-rel-path>` and immediately run `rm -rf artifacts/staged` (`cp -R artifacts/staged/* . && rm -rf artifacts/staged`) so zero scratch files remain.
  2. If `ref/` changed, build `ref/<Lib>.csproj` first (`./dotnet.sh build --no-restore src/libraries/<Lib>/ref/<Lib>.csproj /p:RuntimeConfiguration=Release`, ~3s).
  3. Build `src/<Lib>.csproj` with **`-f <host-tfm>`** (~4–8s, `BinPlace` updates `testhost`) during iterative coding by picking the most specific entry listed in `<TargetFrameworks>` of `src/<Lib>.csproj` for the current host OS:
     - macOS: `<tfm>-osx` if listed → else `<tfm>-unix` if listed → else `<tfm>` (omit `-f` if singular `<TargetFramework>`).
     - Linux: `<tfm>-linux` if listed → else `<tfm>-unix` if listed → else `<tfm>`.
     - Windows: `<tfm>-windows` if listed → else `<tfm>`.
  4. Run filtered xUnit tests via `./dotnet.sh build <TestProject>.csproj /t:Test /p:RuntimeConfiguration=Release` with `/p:XUnitMethodName=...` or `/p:XUnitClassName=...`: **omit `--no-restore` on the FIRST `/t:Test` run** of each test `.csproj` (`clr+libs` does not restore `libs.tests`, so `--no-restore` on run #1 fails with `MSB4057: The target "GetProjectWithBestTargetFrameworks" does not exist`), then pass `--no-restore` on all subsequent `/t:Test` runs of that test `.csproj`.
  5. Run **one** full multi-TFM `src/<Lib>.csproj` build **without `-f`** right before the [author review gate](skills/code-review/author-review.md) (and again if post-review fixes touch `src/` or `ref/`) to verify cross-OS `CA1416`, trimming, and ref/src parity across `windows`/`unix`/`browser`/`wasi`/mobile/downlevel TFMs. Run benchmarks only when performance measurement was requested, keep the author review gate to one bounded pass while budget permits, and re-run build plus filtered tests after any post-review edit.
