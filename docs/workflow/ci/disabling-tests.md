# Disabling tests

This document describes how to disable a test from running in the continuous integration (CI) test system.

## Why disable tests?

Tests are either disabled permanently or temporarily:
- Permanently: the test is never expected to run in a certain configuration, due to the design of the test or product.
- Temporarily: the test is failing, and needs to be disabled so a test run isn't continuously made "noisy" by the
existence of a failing test in the results. These tests are expected to be re-enabled when a bug is fixed or
feature is implemented.

## Runtime or libraries?

There are two main sets of tests in the repo: runtime tests in the src/tests tree, and libraries tests which
are spread amongst the libraries in the src/libraries tree. (There are also PAL tests in src/coreclr/pal/tests, which
are ignored here.)

The two types have very different mechanisms for disabling.

## Test configuration

You need to determine under which configuration you wish to disable the test:
- For all configurations
- For just one processor architecture (x86, x64, arm32, arm64)
- For just one runtime (coreclr, mono) or mono runtime variant (monointerpreter, llvmaot, llvmfullaot)
- For just one operating system (Windows, Linux, macOS, Android, iOS)
- For a particular run type:
   - GCStress
   - JIT stress (any type)
   - ildasm/ilasm round-trip testing
   - ReadyToRun testing

Generally, you should disable a test under the most specific condition that is causing the test failure.
Thus, if the test only fails on arm64, don't disable it for all architectures. If the test only fails
on macOS, don't disable it for Windows or Linux.

If it is unclear the full set configurations where the test is failing, it is sometimes necessary and
expedient to disable the test more broadly than possibly required.

## Disabling runtime tests (src/tests)

Most tests are disabled by adding the test to the appropriate place, under the appropriate configuration condition,
in the [issues.targets](../../../src/tests/issues.targets) file. All temporarily disabled tests must have a
link to a GitHub issue in the `<Issue>` element. Disabling a test here can be conditioned on processor
architecture, runtime, and operating system.

However, some test configurations must be disabled by editing the `.csproj` or `.ilproj` file for the test,
and inserting a property in a `<PropertyGroup>`, as follows:

- Prevent a test from running under GCStress: add `<GCStressIncompatible>true</GCStressIncompatible>`
- Prevent a test from running when testing unloadability: add `<UnloadabilityIncompatible>true</UnloadabilityIncompatible>`
- Prevent a test from running when testing ildasm/ilasm round-tripping: add `<IlasmRoundTripIncompatible>true</IlasmRoundTripIncompatible>`
- Prevent a test from running under HeapVerify: add `<HeapVerifyIncompatible>true</HeapVerifyIncompatible>`
- Prevent a test from running under Mono AOT modes: add `<MonoAotIncompatible>true</MonoAotIncompatible>`
- Prevent a test from running running under JIT stress modes: add `<JitOptimizationSensitive>true</JitOptimizationSensitive>`

Note that these properties can be conditional, e.g.:
```
<GCStressIncompatible Condition="'$(TargetArchitecture)' == 'arm64' and '$(TargetOS)' == 'OSX'">true</GCStressIncompatible>
```

(REVIEW: I'm not clear which conditions are allowed, and respected.)

More information about writing/adding tests to src/tests can be found [here](../testing/coreclr/test-configuration.md).

## Disabling runtime tests (src/tests) with xunit attributes

When this document was written, the src/tests tree was in the process of being converted to a new
execution model (see [dotnet/runtime#54512](https://github.com/dotnet/runtime/issues/54512)). This model annotates tests with xunit-style
`[Fact]` attributes. For tests which have been converted to this form, all the xunit attributes related to test disabling
described in the "src/libraries" section below are also applicable.

## Disabling libraries tests (src/libraries)

Information on disabling libraries tests is found [here](../testing/libraries/filtering-tests.md).

In particular, look at `ActiveIssueAttribute`, `SkipOnCoreClrAttribute`, and `SkipOnMonoAttribute`.