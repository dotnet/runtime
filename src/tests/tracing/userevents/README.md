# User Events Functional Tests

This directory contains **functional tests** for the .NET user_events scenario. These tests validate that .NET Runtime user events can be emitted via EventPipe and collected by [one-collect](https://github.com/microsoft/one-collect/)'s `record-trace` tool.

## High-level Test Flow

Each scenario (for example, `basic`) uses the same pattern:

1. **Scenario invokes the shared test runner**

    User events scenarios can differ in their tracee logic, the events expected in the .nettrace, the record-trace script used to collect those events, and how long it takes for the tracee to emit them and for record-trace to resolve symbols and write the .nettrace. To handle this variance, UserEventsTestRunner lets each scenario pass in its scenario-specific record-trace script path, the path to its test assembly (used to spawn the tracee process), a validator that checks for the expected events from the tracee, and optional timeouts for both the tracee and record-trace to exit gracefully.

2. **`UserEventsTestRunner` orchestrates tracing and validation**

    Using this configuration, UserEventsTestRunner first checks whether user events are supported. It then starts record-trace with the scenario’s script and launches the tracee process so it can emit events. After the run completes, the runner stops both the tracee and record-trace, opens the resulting .nettrace with EventPipeEventSource, and applies the scenario’s validator to confirm that the expected events were recorded. Finally, it returns an exit code indicating whether the scenario passed or failed.

## Layout

- `common/`
  - `UserEventsRequirements.cs` - Checks whether the environment supports user events.
  - `UserEventsTestRunner.cs` - Shared runner that coordinates `record-trace`, the tracee process, and event validation.
  - `userevents_common.csproj` - Common project for shared user events test logic.
- `<scenario>/`
  - `<scenario>.cs` - The tracee workload logic used when invoked with the `tracee` argument.
  - `<scenario>.csproj` - Project file for the scenario.
  - `<scenario>.script` - `record-trace` script that configures how to collect the trace for the scenario.

Each scenario reuses the common runner and shared `record-trace` deployable instead of duplicating binaries or orchestration logic. The `basic` scenario serves as a concrete example of how to add additional scenarios.
