---
name: test-quarantine
description: >
  Narrowly quarantine failing dotnet/runtime tests while preserving unaffected
  coverage. USE FOR: disable a failing test, skip a test against an issue, add
  ActiveIssue, quarantine a flaky configuration, split failing theory data,
  suppress a runtime-test failure, or handle a failure in a merged runner.
  DO NOT USE FOR: fixing the underlying product bug, deleting low-value tests,
  or broadly disabling a CI lane.
---

# Narrow Test Quarantine

Temporarily suppress only the failure covered by an active tracking issue.
Preserve every unaffected test, data row, runtime, platform, architecture,
configuration, and execution mode.

## When to Use

Use this skill when a failing test must be disabled in source while its
underlying issue is fixed, including:

- a test method failing only in a specific configuration
- some theory rows failing while others pass
- one subtest failing inside a manually dispatched test
- a runtime test inside a merged runner
- a failure that occurs before the test method executes
- a test with a custom `Main`, checked-in IL, or another shape that cannot use
  normal xUnit filtering

## Stop Signals

Stop and do not create a quarantine when:

- the user asked to fix the underlying bug rather than disable the test
- current evidence shows the failure is already fixed
- there is no active issue describing the failure and re-enable condition
- the proposed change would disable a whole lane or unrelated work item
- the exact failing test or failure phase cannot be identified

Ask for a decision only when a broader suppression is genuinely required and
multiple scopes would discard materially different coverage.

## Core Rules

1. **Use fresh evidence.** Confirm the failure on current `main` or the branch
   being changed. Do not quarantine stale failures.
2. **Classify the failure phase before selecting a mechanism.** Distinguish
   source compilation, AOT/ReadyToRun compilation, linking, startup, dispatch,
   and test execution. An execution-time attribute cannot suppress an earlier
   failure.
3. **Resolve the narrowest failing unit.** Prefer, in order:
   - theory row or manually invoked subtest
   - test method
   - test class
   - child project
   - runner or work item
   - CI lane

   Moving to a broader level requires evidence that the narrower level cannot
   intercept the failure.
4. **Use an active tracking issue.** New temporary quarantines must link to an
   open issue covering the specific failure and its re-enable condition. Do
   not use a closed issue, PR URL, or `needs triage`.
5. **Prefer existing predicates and filtering mechanisms.** Search nearby tests
   and shared test utilities before adding a new condition.
6. **Prove retained coverage.** Validation must show both that the intended
   failure is suppressed and that the nearest unaffected case still executes.

## Step 1: Read the Test Infrastructure

Treat the repository documentation as the source of truth for available
attributes, properties, and isolation requirements. This skill does not replace
those inventories; it connects them into a narrowness decision process.

Read:

- [`docs/workflow/ci/disabling-tests.md`](../../../docs/workflow/ci/disabling-tests.md)
- [`docs/workflow/ci/failure-analysis.md`](../../../docs/workflow/ci/failure-analysis.md)
- the nearest test-area `README.md`
- `.github/instructions/tests.instructions.md`
- the language and area instruction files matching the changed files

For library-test attributes, also consult
[`docs/workflow/testing/libraries/filtering-tests.md`](../../../docs/workflow/testing/libraries/filtering-tests.md).

For runtime-test project properties or merged-runner behavior, also consult:

- [`docs/workflow/testing/coreclr/requiresprocessisolation.md`](../../../docs/workflow/testing/coreclr/requiresprocessisolation.md)
- the owning project and parent runner
- `src/tests/MergedTestRunner.targets`
- `src/tests/Directory.Build.targets`

Invoke the `build-and-test` skill before running a build or test command.

## Step 2: Establish the Failure Contract

Record:

- exact test method, data row, or subtest
- owning project
- runner or work item, if different from the project
- affected runtime, platform, architecture, build configuration, and test mode
- failure phase
- exact failure signature
- active tracking issue

For a merged runner, keep these identities separate:

1. work item or parent runner
2. referenced child project or assembly
3. generated test case or method

Do not suppress a runner merely because one referenced test failed.

### Confirm execution from test evidence

Do not infer that a test ran or passed from:

- a successful build
- successful Helix submission
- a work-item exit code
- the presence of an assembly in output

Inspect the generated wrapper, runner log, and xUnit XML/TRX for the exact test.
Fatal crashes may prevent results from being published; merged wrappers may
record failures while their outer process still returns its expected success
code. Use the narrowest available runner or test filter when an early failure
prevents later tests from executing.

## Step 3: Choose the Correct Mechanism

### Temporary execution failure

Use method-level `ActiveIssue` when the test method is discovered and the
failure occurs during execution:

```csharp
[ActiveIssue(
    "https://github.com/dotnet/runtime/issues/12345",
    typeof(TestLibrary.PlatformDetection),
    nameof(TestLibrary.PlatformDetection.SomeAffectedCondition))]
```

Prefer a condition that describes the failing configuration rather than a
broader platform or runtime category.

### Permanent unsupported scenario

Do not use `ActiveIssue` when the test is never expected to work in that
configuration. Use the established permanent filtering mechanism:

- `PlatformSpecific`, `SkipOnPlatform`, or `SkipOnTargetFramework`
- `ConditionalFact` or `ConditionalTheory` with a capability predicate
- a runtime-specific skip such as `SkipOnCoreClr` or `SkipOnMono`

The reason should describe the intentional limitation.

### Runtime test mode

Use the filter that directly represents the failing mode. Examples include
`SkipOnCoreClr` with `RuntimeTestModes`, `RuntimeConfiguration`, or both for
GCStress, JIT stress, minopts, and checked/debug-only failures.

Do not approximate a test mode using an OS or architecture predicate.

Prefer these xUnit mode filters over `GCStressIncompatible` or
`JitOptimizationSensitive`. Use those project properties only when the test
already requires process isolation for another documented reason; they should
not be the reason process isolation is introduced.

### Only some theory rows fail

`ActiveIssue` applies to the generated test method, not an individual
`InlineData` or `MemberData` row. Preserve passing rows:

1. Split affected rows into a second theory method carrying the quarantine.
2. Share the implementation in a helper.
3. Keep unaffected data in the original active theory.

Do not quarantine the whole theory when a smaller data partition is possible.

### One manually invoked subtest fails

An attribute on an umbrella method suppresses every subtest it invokes. Guard
only the failing invocation with an existing configuration predicate and
preserve the established skipped-test reporting. Leave sibling invocations
active.

### Custom `Main`, checked-in IL, or no generated xUnit dispatch

If xUnit attributes cannot intercept the test, use the established
project-level mechanism. For a process-isolated runtime test, condition
`CLRTestTargetUnsupported` as narrowly as possible and include the issue link
or reason.

If the project expects a non-default `CLRTestExitCode`, do not use an
xUnit-level skip. A skipped xUnit test returns the normal success code and can
conflict with the process-level expected exit code. Keep all skip logic for
such tests in the project or execution script.

When a temporary quarantine does not use the `ActiveIssue` attribute, put an
adjacent comment containing the literal `ActiveIssue` and the full tracking
issue URL. This keeps the suppression discoverable by repository searches and
closed-issue reference checks:

```xml
<!-- ActiveIssue https://github.com/dotnet/runtime/issues/12345 -->
<CLRTestTargetUnsupported Condition="...">true</CLRTestTargetUnsupported>
```

Keep checked-in IL and any C# reference source synchronized when both describe
the executable test contract.

### Failure before test execution

`ActiveIssue` does not prevent the assembly from being built, linked, or passed
to an ahead-of-time compiler. Consult `disabling-tests.md` for the project
property matching the failing phase, then verify its isolation requirement in
`requiresprocessisolation.md`.

Two distinctions repeatedly cause incorrect quarantines:

- `CrossGenTest=false` prevents Crossgen compilation, while
  `R2RIncompatible=true` skips ReadyToRun execution. They are not
  interchangeable.
- Properties implemented in a per-test script require the project to own that
  script, often through `RequiresProcessIsolation`. A merged child does not own
  the parent runner's script, so child properties do not automatically affect
  the parent.

Condition the selected property to the exact failing configuration. Do not add
process isolation for a method-level `ActiveIssue`, and do not add it merely to
bypass correct merged execution.

For every temporary project-property exclusion, add an adjacent
`<!-- ActiveIssue https://github.com/dotnet/runtime/issues/<number> -->`
comment. Do not rely on an unexplained property or a bare issue number; the
literal marker and full URL make the exclusion grep-discoverable.

## Step 4: Construct Narrow Conditions

Prefer predicates describing capabilities or execution state over raw
platform names. Combine independent predicates when no single existing
condition precisely describes the failure.

Use the detection helper owned by the test infrastructure being edited.
Runtime tests and library tests have different `PlatformDetection` and
`Utilities` types; identically named members can differ or be absent.

The runtime-test wrapper generator combines multiple user-defined condition
member names with logical **AND**:

```csharp
[ActiveIssue(
    "https://github.com/dotnet/runtime/issues/12345",
    typeof(TestLibrary.PlatformDetection),
    nameof(TestLibrary.PlatformDetection.ConditionA),
    nameof(TestLibrary.PlatformDetection.ConditionB))]
```

For **library tests**, use enum overloads only when their dimensions completely
describe the scope:

```csharp
[ActiveIssue(
    "https://github.com/dotnet/runtime/issues/12345",
    TestPlatforms.Browser,
    TargetFrameworkMonikers.Any,
    TestRuntimes.Mono)]
```

`TargetFrameworkMonikers.Any` is the all-framework placeholder required by
this overload while retaining the platform and runtime restrictions.

Do not copy this overload into `src/tests`. The runtime-test wrapper generator
applies its platform, target-framework, and runtime filters independently
rather than as one AND condition. When a runtime test needs multiple axes to
hold simultaneously, use the user-defined predicate form above.

### Representative configuration example

If a test fails only in WebAssembly ReadyToRun, a Browser-only attribute is
too broad because it also suppresses browser interpreter coverage. Reuse the
shared `IsWasm` and `IsReadyToRunCompiled` predicates so both conditions must
hold. The general pattern is what matters: represent every independent
dimension of the failing configuration and preserve the nearest passing mode.

When adding a shared predicate is unavoidable, base it on an established test
contract, ensure every applicable lane sets it, and verify it is visible inside
the managed test process. Do not create an ad hoc environment variable for one
test.

## Step 5: Account for Merged Runners

Method-level `ActiveIssue` is honored by `XUnitWrapperGenerator` in merged
runtime-test runners, but project-level properties have different semantics:

- The parent runner may be built once and reused across execution modes.
- Child properties do not automatically change parent runner compilation or
  invocation.
- Removing a child project can discard coverage from every mode.
- `RequiresProcessIsolation` creates a standalone execution path and must be
  justified by the test contract or the selected project-level filter.

Inspect the generated `FullRunner.g.cs` or equivalent wrapper to confirm the
test is discovered and the condition is emitted as intended. For a
project-level exclusion, prove the failing input is absent while sibling
projects remain present.

## Step 6: Validate Narrowness

1. Build with the command selected by the `build-and-test` skill.
2. Run the exact failing configuration.
3. Confirm suppression in the form produced by that test infrastructure:
   - runtime wrappers should report the skip and issue URL
   - library `ActiveIssue` tests may be filtered from the result set by the
     `failing` category rather than reported as skipped
   - pre-execution exclusions should omit only the intended input
4. Run or inspect the nearest unaffected configuration and confirm the test
   still executes.
5. For split theories, confirm unaffected rows still run.
6. For umbrella tests, confirm sibling subtests still run.
7. For merged runners, inspect generated dispatch and structured test results,
   not only process exit status.

If the affected environment cannot run locally, validate generated dispatch,
evaluated project properties, and the exact CI invocation. State that live
execution remains unverified.

## Step 7: Post-Merge Issue Hygiene

Do not apply issue labels merely because a quarantine PR is open. Once the PR
is merged:

1. Add the **`disabled-test`** label to every issue that covers a specific
   failure disabled by the merged change. The label means: “The test is
   disabled in source code against the issue.”
2. If one PR adds quarantines against multiple tracking issues, update every
   referenced issue, not only the PR's primary issue.
3. Remove `blocking-clean-ci` or related blocking labels when the merged
   quarantine means the issue no longer blocks clean CI, following
   `failure-analysis.md`.
4. Do not close the product issue solely because the test is disabled. The
   issue remains the re-enable tracker until the underlying failure is fixed.

## Step 8: Review the Change

Reject or revise a quarantine that:

- skips a broader unit than the failure requires
- suppresses passing data rows or sibling subtests
- disables an unrelated runtime, platform, architecture, configuration, or
  test mode
- uses a closed or unrelated issue
- uses `ActiveIssue` for a pre-execution failure
- adds process isolation without a documented reason
- uses a temporary non-attribute exclusion without an adjacent
  grep-discoverable `ActiveIssue` comment and full issue URL
- trusts a runner exit code without structured test evidence
- changes a shared predicate without validating its consumers

Run the `code-review` skill on the completed changes and address all errors and
warnings.

## Final Response

Report:

- tracking issue or issues
- exact test unit quarantined
- exact configuration condition
- mechanism used and why narrower mechanisms could not work
- coverage explicitly preserved
- validation of both suppression and retained execution
- whether post-merge `disabled-test` labeling remains pending

Do not describe a whole runner or work item as disabled when only one child
test was quarantined.
