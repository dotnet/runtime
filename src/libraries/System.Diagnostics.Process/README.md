# System.Diagnostics.Process
Contains the source and tests of assembly System.Diagnostics.Process which includes a subset of members of the System.Diagnostics namespace that allow you to interact with local and remote system processes.

Documentation can be found at https://learn.microsoft.com/dotnet/api/system.diagnostics. The primary class is [`Process`](https://learn.microsoft.com/dotnet/api/system.diagnostics.process).

## Contribution Bar
- [x] [We only consider fixes to maintain or improve quality](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bar)

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aopen+is%3Aissue+label%3Aarea-System.Diagnostics.Process+label%3A%22help+wanted%22) issues.

## Deployment
`System.Diagnostics.Process` is included in the shared framework. The package does not need to be installed into any project compatible with .NET Standard 2.0.

## Helix test partitions

Windows x86 CoreCLR console-runner test archives are divided into six Helix work items.
Each archive contains the same test assembly and supporting files, with a class filter in its
generated runner script. Each work item has its own payload, results, temporary files, and
test process. The existing assembly-level collection behavior is unchanged within each process.

The partitions are balanced using matched x86 CI timings, with headroom for slower executions
and work-item setup. The goal is five minutes or less per work item; the normal Helix timeout
is not reduced or increased. All rows of a theory stay together with their class.
The final partition excludes the classes in the first five, so new classes remain covered.

The ordinary local runner still executes the full suite. Other architectures, Mono, mobile,
NativeAOT, and other single-file runners keep their existing unpartitioned archives.
