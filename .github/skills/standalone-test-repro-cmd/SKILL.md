---
name: standalone-test-repro-cmd
description: >
  Resolve a CoreCLR runtime test from a generated .cmd or .sh wrapper path for
  the standalone-test-repro workflow. Use when the requested test is identified
  by a CI or Helix wrapper path, with or without a named test scenario.
  Continue with the parent standalone-test-repro skill after resolution.
---

# Resolve a Runtime Test Wrapper

This is the wrapper-path resolution sub-skill for
[`../standalone-test-repro/SKILL.md`](../standalone-test-repro/SKILL.md).
Resolve the exact source test, owning project, and effective invocation, then
continue with the parent's shared extraction steps.

## Input

Accept a generated `.cmd` or `.sh` path copied from a CI failure, for example:

```text
JIT\Directed\ConvertToInt\checked\ConvertToInt.cmd
```

The path may be absolute, relative to a Helix work item or runtime test output
root, and may use either directory separator. The surrounding request may also
name a scenario such as `jitstress1`; preserve it separately from the path for
the parent workflow.

## Resolution

1. Normalize the identifier:
   - Trim quotes, Markdown formatting, leading test-result labels, and trailing
     punctuation.
   - Remove command-line arguments while retaining the path through `.cmd` or
     `.sh`.
   - Convert separators only for host filesystem operations.
   - Do not treat a separately supplied scenario as part of the wrapper path.
2. If the wrapper exists locally, read it and record:
   - The managed assembly or executable
   - `corerun` arguments, including `-p` and `-e`
   - Test arguments and expected exit code
   - Environment variables
   - Target-specific batch or Bash pre/post commands
   - Working-directory assumptions
   - Copied data or native dependencies
3. Map an artifact or Helix path back to `src/tests/`:
   - Strip prefixes through the runtime test output root.
   - Remove configuration-only artifact path segments.
   - Search the remaining relative directory for the wrapper stem, likely
     `.csproj`, source file, and matching `AssemblyName`.
4. If the wrapper is unavailable, use the path and stem to search under
   `src/tests/`; derive invocation settings from the project.
5. Inspect the owning project, applicable imports, and its explicit or evaluated
   `Compile` items. Evaluate target-specific environment items and
   `CLRTestBatchPreCommands`, `CLRTestBashPreCommands`,
   `CLRTestBatchPostCommands`, and `CLRTestBashPostCommands`.
6. Confirm the assembly contains only the requested runnable test. If it
   contains multiple independently runnable tests, stop and ask for a fully
   qualified method name.

Do not assume a wrapper only runs `corerun Test.dll`. Its host options,
environment, arguments, setup, and working directory may be required to
reproduce the failure. When available, the generated wrapper is the ground
truth for the effective invocation.

Resolution is complete only when the exact source method, owning project, and
effective invocation are known. Return any requested scenario unchanged for
canonicalization by the `test-scenario-env` skill.
