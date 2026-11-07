# Host, Tools, and Build Tasks

Covers `src/native/corehost/`, `src/installer/`, `src/tools`, `src/native/managed`, and
`src/tasks`. Confirm the relevant baseline sentinel in
[the build-and-test guide](../SKILL.md#baseline-sentinels) first. Managed
ILAssembler has its own focused baseline and workflow below.

## Host

**Build:** `./build.sh host -rc release -lc release`

**Test:** `./build.sh host.tests -rc release -lc release -test`

## Managed ILAssembler

For changes under `src/tools/ilasm/**`, including the managed library, CLI, tests,
targeting, or polyfills, use the focused subset rather than the general Tools baseline.

**Build:** `./build.sh tools.ilasm`

**Test:** `./build.sh tools.ilasm -test`

On Windows, use `.\build.cmd tools.ilasm` and `.\build.cmd tools.ilasm -test`.
The subset builds the ILAssembler library, CLI, package, and test project. Linking
shared source from another component does not by itself require that component's
baseline; add one only when the changed code's validation actually depends on its outputs.

## Tools

**Build:** `./build.sh tools+tools.ilasm`

**Test:** `./build.sh tools+tools.ilasm+tools.illinktests+tools.cdactests -test`

These combined commands include broader tool coverage. Use the focused Managed
ILAssembler commands above when only that component is being changed.

## Build Tasks

**Build:** `./build.sh tasks`

No baseline is required — this is self-contained. If you go on to consume the tasks from a
workflow that does need one (e.g. libraries tests), apply that workflow's sentinel instead.

## Reference

- [Host Tests](/docs/workflow/testing/host/testing.md)
