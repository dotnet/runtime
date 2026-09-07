---
name: standalone-test-repro-method
description: >
  Resolve a CoreCLR runtime test from a fully qualified method name for the
  standalone-test-repro workflow. Use for xUnit display names and methods in
  merged or multi-test assemblies, with or without a named test scenario.
  Continue with the parent standalone-test-repro skill after resolution.
---

# Resolve a Runtime Test Method

This is the method-name resolution sub-skill for
[`../standalone-test-repro/SKILL.md`](../standalone-test-repro/SKILL.md).
Resolve the exact source test, owning project, and test invocation, then
continue with the parent's shared extraction steps.

## Input

Accept a fully qualified method name copied from a CI failure, for example:

```text
Namespace.TypeName.MethodName
```

It may include an assembly prefix, xUnit display-name arguments, quotes,
parentheses, nested-type notation, or surrounding CI log text. The surrounding
request may also name a scenario such as `jitstress1`; preserve it separately
from the method identifier for the parent workflow.

## Resolution

1. Normalize the identifier:
   - Trim quotes, Markdown formatting, leading result labels, and trailing
     punctuation.
   - Preserve xUnit argument text for theory-case resolution.
   - Remove an assembly prefix only after verifying it is an assembly name.
   - Never reduce the identifier to only the final method segment.
   - Do not treat a separately supplied scenario as part of the method name.
2. Parse the longest plausible namespace/type prefix and method name. Account
   for nested types (`+` in reflection names and `.` in C# source) and generic
   arity suffixes.
3. Search C# sources under `src/tests/` for the exact method declaration and
   confirm its containing type and namespace. Prefer symbol information when
   available; otherwise combine targeted searches for method, type, and
   namespace.
4. Find the owning `.csproj` by its explicit or evaluated `Compile` items.
   Account for wildcard includes and merged projects such as JIT regression
   assemblies.
5. Inspect the project and source for:
   - `[Fact]`, `[Theory]`, custom attributes, or `TestEntryPoint`
   - Static or instance invocation and constructor requirements
   - Fixtures, setup, cleanup, and disposal
   - Async and generic return or argument handling
   - Conditional compilation and platform guards
   - Test data and the specific failing theory row
   - Target-specific environment items and batch or Bash pre/post commands
6. If display-name arguments identify a theory row, reproduce exactly that
   row. If not, generate invocations for all locally resolvable rows and label
   them. Use the `ask_user` tool when runtime-discovered data or ambiguity
   prevents a faithful choice.
7. If multiple source methods still match, present the concrete candidates with
   `ask_user`; do not guess.

Resolution is complete only when the exact source method, owning project, and
required invocation semantics are known. Return any requested scenario
unchanged for canonicalization by the `test-scenario-env` skill.
