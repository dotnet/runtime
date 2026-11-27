# Bug Report Triage

Detailed guidance for triaging bug reports in dotnet/runtime. Referenced from
the main [SKILL.md](../SKILL.md) during Step 5.

## Reproduction

> [!] **Safety gate:** If Step 0b of the main workflow flagged any safety
> concerns with the reproduction code, **skip reproduction entirely**. Do not
> execute code that was flagged as suspicious, even partially.

### Verify before validating

Before attempting to reproduce the bug, check whether the reported behavior
actually contradicts the documented API contract:

1. **Read the API documentation** for the type or method in question. Does the
   docs page describe the behavior the author is seeing? If so, this may be
   "by design" rather than a bug.
2. **Check for documented preconditions** -- Does the API require specific input
   formats, minimum sizes, non-null arguments, or particular configurations
   that the author may not be meeting?
3. **Look for "Remarks" or "Exceptions" sections** in the API docs -- these
   often document edge cases that callers misinterpret as bugs.
4. **Search for prior discussions** -- Has this exact behavior been discussed
   before and confirmed as intentional? Check closed issues with the same area
   label.

If the behavior matches the documented contract, recommend **CLOSE** with a
"by design" rationale. Explain the API contract clearly, link to the relevant
documentation, and suggest the correct usage pattern. This is one of the most
valuable triage outcomes -- it saves maintainers from having to investigate
non-bugs.

### Evaluate repro quality

Before attempting reproduction, assess the quality of the reproduction provided
in the issue. This determines both how to proceed and whether to request a
better repro.

| Quality | Criteria | Action |
|---------|----------|--------|
| **Good** | Public GitHub repo or inline code snippet, minimal (no unnecessary deps), targets a specific TFM, includes expected vs. actual behavior, no binaries or zip files | Proceed with reproduction |
| **Weak** | Complex multi-project solution, requires external services or databases, code screenshot, zip attachment, private repo, includes unnecessary dependencies | Note the weakness; attempt reproduction if feasible, otherwise request a better repro |
| **None** | No code at all, only a description of the problem | Skip reproduction; recommend NEEDS INFO with specific request for a minimal repro |

If the repro is weak or absent, include specific guidance in the NEEDS INFO
response about what a good repro looks like (inline code or public GitHub repo,
minimal dependencies, specific TFM, expected vs. actual behavior).

### When to reproduce

| Scenario | Action |
|----------|--------|
| Bug in released .NET version (e.g., .NET 10) | Create a temp console app with `dotnet new console`, add repro code, `dotnet run` |
| Bug in preview/nightly bits | May require a full repo build -- warn the user about time cost first |
| Environment mismatch (e.g., macOS-only bug, running on Windows) | **Do NOT attempt.** State: "Unable to independently verify -- this issue reports a [macOS/Linux/ARM64]-specific problem and the current environment is [Windows/x64]." |
| No repro steps provided | Note the missing information in the NEEDS INFO recommendation |
| Repro requires external services, hardware, or complex setup | **Do not attempt.** Note the limitation. |

### How to reproduce

1. Create a temporary directory
2. `dotnet new console -n TriageRepro`
3. Replace `Program.cs` with the minimal reproduction from the issue
4. `dotnet run` (or `dotnet test` if it's a test-related issue)
5. Compare output to expected vs. actual behavior described in the issue
6. Clean up the temporary directory when done

### Interpreting results

- **Reproduced**: Confirms the bug is real. Note the .NET version and environment.
- **Could not reproduce**: Doesn't mean the bug doesn't exist -- note your environment
  and .NET version. The bug may be environment-specific.
- **Not reproducible with good repro**: If a good-quality reproduction was provided but
  the bug cannot be reproduced on the current environment and .NET version, this is a
  stronger signal. Note the environment difference and consider whether the issue is
  environment-specific or already fixed.
- **Build/compilation error in repro**: The repro steps may be incomplete or outdated.

## Regression Validation

If the bug reproduces (or the issue claims it's a regression), verify whether it
worked in a previous .NET release. This is critical -- regressions are treated with
higher priority by maintainers.

1. **Check the issue text** -- does the author say "this worked in .NET X"?
2. **Test against the previous release** -- create the same repro project targeting
   the prior TFM (e.g., `net9.0` vs `net10.0`). Edit the `.csproj` to change
   `<TargetFramework>` and re-run. If the prior SDK isn't installed, use
   `global.json` to pin to an older SDK version, or use a Docker image.
3. **Determine the regression window**:
   - Works on .NET 9, fails on .NET 10 → regression introduced in .NET 10
   - Works on .NET 10 GA, fails on .NET 10.x servicing → servicing regression
   - Fails on both .NET 9 and .NET 10 → long-standing bug, not a regression
4. **Report findings clearly** in the Reproduction section of the triage report:
   - [ok] **Confirmed regression** from .NET {old} → .NET {new}.
   - [x] **Not a regression** -- also fails on .NET {old}.
   - [!] **Unable to verify regression** -- {reason, e.g., prior SDK not available}.

> Regressions should generally be recommended as **KEEP** with elevated priority.

## Derive a Minimal Reproduction

If the bug was successfully reproduced but the reproduction provided in the issue
is not minimal (e.g., it relies on a large JSON file, a zip attachment, a
multi-project solution, or contains unnecessary types and dependencies), derive a
**minimal self-contained reproduction** that can be pasted inline into the issue.

### When to skip minimization

Skip minimization entirely if any of these conditions apply:

- The original reproduction is already small (roughly 30 lines or fewer)
- The recommendation will be CLOSE or NEEDS INFO (minimization adds value
  primarily for issues that will remain open)
- Reproduction was not attempted (e.g., environment mismatch, safety concerns)
- The issue is a duplicate of an existing issue that already has a minimal repro

As stated in the project's
[`CONTRIBUTING.md`](../../../../CONTRIBUTING.md#why-are-minimal-reproductions-important),
minimal reproductions are important because they:

1. Focus debugging efforts on a simple code snippet,
2. Ensure that the problem is not caused by unrelated dependencies/configuration,
3. Avoid the need to share production codebases.

A good minimal reproduction (per `CONTRIBUTING.md`):

- Excludes all unnecessary types, methods, code blocks, source files, NuGet
  dependencies, and project configurations.
- Contains documentation or code comments illustrating expected vs. actual
  behavior.
- Is fully self-contained: running it (`dotnet run`) should immediately
  demonstrate the unexpected behavior without requiring additional manual
  steps or external input (e.g., submitting a web request, reading from a
  file, attaching a debugger). Prefer a plain console app that exercises the
  bug directly and prints expected vs. actual output.

### The minimization algorithm

Apply this iterative removal algorithm to systematically reduce the reproduction
to its minimal form:

```
1. Identify all "aspects" of the reproduction that could potentially be removed
   or simplified. An aspect is any independently removable element:
   - A type/class in the model
   - A property on a type
   - A nesting level in the data
   - A specific input value (can it be simplified? e.g., a long string → "x")
   - A dependency or configuration option
   - A code construct (e.g., `required` keyword, `readonly`, `record` vs `class`)

2. For each aspect, test its removal:
   a. Remove or simplify the single aspect
   b. Run the reproduction
   c. If the bug STILL triggers → the aspect is superfluous; keep it removed
   d. If the bug DISAPPEARS → the aspect is essential; undo the removal

3. After completing a full pass over all aspects, if any aspects were removed
   in this round, start a new round from step 1 (removing an aspect may make
   other previously-essential aspects now superfluous).

4. When a full round completes with zero removals, the reproduction is minimal.
```

### Minimization priorities

When minimizing, prioritize reducing these dimensions (in order):

1. **Model complexity** -- Reduce the number of types/classes first. Fewer types
   is the single biggest readability win. Explore whether the same bug triggers
   with a simpler model by varying other parameters (e.g., different buffer
   sizes, different input shapes).
2. **Input data** -- Shrink input data (JSON, XML, payloads) to the smallest
   string or value that still triggers the bug. Try both shortening values and
   removing fields.
3. **Code constructs** -- Simplify types: prefer `class` over `record`,
   `get; set;` over `get; init;`, drop `required`, `readonly`, etc. -- unless
   the construct itself is essential to triggering the bug.
4. **Dependencies and configuration** -- Remove NuGet packages, project
   configuration, and API options that aren't required.

### Stabilizing the reproduction

Some bugs do not trigger reliably under default conditions. Before minimizing,
you must first make the reproduction deterministic. Common sources of
instability and how to address them:

- **Buffer or chunk boundaries** -- The bug only triggers when data crosses an
  internal boundary at a specific offset. Vary parameters that control
  boundaries (buffer sizes, chunk sizes, stream read lengths, etc.) -- a
  different value may allow a simpler model or shorter input. Document which
  parameter values trigger the bug and which don't.
- **Input size sensitivity** -- When shrinking a value causes the bug to
  disappear, it may be a boundary-alignment issue rather than a value-content
  issue. Try padding with different approaches to find the shortest overall
  reproduction.
- **Thread scheduling and race conditions** -- The bug manifests
  nondeterministically due to interleaving. Wrap the scenario in a loop that
  repeats many times and exits on first failure. Use synchronization primitives
  (`ManualResetEventSlim`, `Barrier`, `SemaphoreSlim`) to force specific
  thread orderings. Increase the number of concurrent tasks beyond what the
  original reproduction uses and eliminate delays that accidentally serialize
  operations.
- **Timing-dependent behavior** -- The bug depends on timeouts, clock
  resolution, or operation ordering. Pin the relevant timing parameters to
  values that trigger the bug consistently.

**Goal:** Run the harness several times and confirm the bug triggers
consistently (>90% of runs). Once determinism is achieved, proceed with the
standard minimization algorithm on the core logic. If the bug remains flaky,
document the failure rate and provide the best harness you can.

## Root Cause Analysis

After reproducing the bug (and optionally minimizing the reproduction), attempt
a lightweight root cause analysis. This is not a full debugging session -- the
goal is to identify the likely area of code responsible, to help maintainers
prioritize and assign the issue.

Include in the report:

- **Likely mechanism** -- A 1-3 sentence hypothesis of what goes wrong.
- **Related code changes** -- If `git log` revealed a relevant commit in the
  regression window, link to it.

If root cause analysis is inconclusive, say so -- a failed investigation is still
useful context ("examined X and Y, but the root cause is not obvious from static
analysis alone").

## Bug-Specific Assessment

When assessing a bug report in Step 6, consider:

- Is this a regression from a previous .NET version?
- How many users are likely affected? (Check +1 reactions, linked issues, external references)
- Is there a workaround?
- How severe is the impact? (crash vs. cosmetic vs. silent data corruption)

## Bug-Specific Recommendation Criteria

### KEEP

- Bug is confirmed or plausible, with sufficient information to act on
- Regressions warrant elevated priority

### CLOSE

- **Not reproducible** -- Bug report includes a good-quality repro, but the issue
  cannot be reproduced on current .NET version. May already be fixed.

### NEEDS INFO

- Bug report lacks reproduction steps
- Bug report lacks environment details (.NET version, OS, architecture)
