# System.Text.RegularExpressions
This assembly provides regular expression functionality that may be used from any platform or language that runs within .NET. The main type contained in the assembly is [Regex](https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.regex) class, which represents an immutable regular expression object which can be used to test for matches on given input strings or spans.

Documentation can be found at https://learn.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.

## Contribution Bar
- [x] [We consider new features, new APIs and performance changes](../../libraries/README.md#primary-bar)
- [x] [We consider PRs that target this library for new source code analyzers](../../libraries/README.md#secondary-bars)

Most of the investments on this library, ever since it got created 20 years ago, have been performance oriented until .NET 7.0 which introduced a lot of functionality changes particularly in APIs that provide low allocations (by adding span support) as well as new Regex engines like the Source generated and the Non-backtracking engines which enable some new scenarios. We plan on continuing our focus on performance as well as continue to add new APIs and features to the new engines.

See the [Help Wanted](https://github.com/dotnet/runtime/issues?q=is%3Aissue+is%3Aopen+label%3A%22help+wanted%22+label%3Aarea-System.Text.RegularExpressions) issues.

## Deployment
System.Text.RegularExpressions assembly is part of the shared framework, and ships with every new release of .NET.
