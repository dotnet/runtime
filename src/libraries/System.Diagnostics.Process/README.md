# System.Diagnostics.Process
Contains the source and tests of assembly System.Diagnostics.Process which includes a subset of members of the System.Diagnostics namespace that allow you to interact with local and remote system processes.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.diagnostics. The primary class is [`Process`](https://learn.microsoft.com/dotnet/api/system.diagnostics.process).

## Contribution Bar
- [x] [We only consider fixes to maintain or improve quality](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-System.Diagnostics.Process+label%3A%22help+wanted%22) issues.

## Deployment
`System.Diagnostics.Process` is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.

## Test isolation
The tests use the default xUnit class-level parallelism in a single test project.
Tests that change or observe process-wide state in ways that depend on other tests not running
must use `RemoteExecutor`. On Windows, remote processes inherit the parent's console, so tests
that change console code pages must also allocate a private console. Temporary files and
directories should use the per-test paths provided by `FileCleanupTestBase`.
Unix single-file runners do not support `RemoteExecutor`, so their `ProcessTests` collection
remains nonparallel to preserve the working-directory test without skipping it.
