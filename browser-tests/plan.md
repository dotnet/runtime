# Browser/WASM CoreCLR Library Tests Plan

## Overview

This document tracks progress for running .NET library tests on the Browser/WASM target with the **CoreCLR virtual machine** (interpreter mode, no JIT, single-thread).

For detailed execution instructions, see 
- [before-testing.md](before-testing.md)
- [test-suite.md](test-suite.md).


## Target Platform Characteristics

| Characteristic | Value |
|----------------|-------|
| OS | Browser (WebAssembly) |
| VM | CoreCLR (interpreter only, no JIT) |
| Threading | **Not supported** - no thread creation, no blocking waits |
| Known Issues | C# finalizers don't work, GC memory corruption bugs |
| Test Runner | Xharness (local web server + Chrome browser) |

## Reference Baseline

The same tests already pass on **Mono + Browser**. Results are in:
- [Mono-chrome-workitems.json](Mono-chrome-workitems.json)

Each work item has a `DetailsUrl` that links to Helix logs with `ConsoleOutputUri` showing test summaries.

## Goals

1. Run all library test suites on Browser/WASM + CoreCLR
2. Compare results with Mono baseline
3. Mark failing tests with `[ActiveIssue("https://github.com/dotnet/runtime/issues/123011")]`
4. Document each failure with full test name and stack trace in `/browser-tests/failures/`

## Decisions Made

| Question | Decision |
|----------|----------|
| GitHub Issue | Use single umbrella issue **#123011** for all Browser+CoreCLR failures |
| Build Configuration | **Debug** - for better stack traces |
| Failure Categories | Decide when all failures collected (threading, gc, finalizer, interpreter, other) |
| Automation | Keep simple, improve as we go |
| Timeouts | Keep current defaults (`WasmXHarnessTestsTimeout` = 00:30:00) |

## Progress Tracking

### Status Legend

- ⬜ Not started
- 🔄 In progress
- ✅ All tests passing (matches or exceeds Mono baseline)
- ⚠️ Tests marked with ActiveIssue
- ❌ Blocked

### Completed Test Suites

| Test Suite | CoreCLR | Mono Baseline | Status |
|------------|---------|---------------|--------|

### In Progress

_None currently_
