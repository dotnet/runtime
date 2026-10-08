# Host, Tools, and Build Tasks

Covers `src/native/corehost/`, `src/installer/`, `src/tools`, `src/native/managed`, and
`src/tasks`. Confirm the component's [baseline sentinel](../SKILL.md#baseline-sentinels) first.

## Host

**Build:** `./build.sh host -rc release -lc release`

**Test:** `./build.sh host.tests -rc release -lc release -test`

## Tools

**Build:** `./build.sh tools+tools.ilasm`

**Test:** `./build.sh tools+tools.ilasm+tools.illinktests+tools.cdactests -test`

## Build Tasks

**Build:** `./build.sh tasks`

No baseline is required — this is self-contained. If you go on to consume the tasks from a
workflow that does need one (e.g. libraries tests), apply that workflow's sentinel instead.

## Reference

- [Host Tests](/docs/workflow/testing/host/testing.md)
