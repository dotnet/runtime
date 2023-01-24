## Automated Issue Cleanup

dotnet/runtime is very popular repository, with tens of issues being filed by the community every day. While we generally do try to respond to and resolve issues as quickly as possible, it is still likely that some issues can be left to stagnate in the backlog. Currently, dotnet/runtime contains hundreds of issues that have not seen any activity in over three years.

In our attempt to create leaner and more focused backlogs, we have implemented automation that identifies stale issues and marks them for closure. This uses a two-phase process: stale issues are [given a notification](https://github.com/dotnet/runtime/issues/7780#issuecomment-1093721931) and marked with the [`backlog-cleanup-candidate`](https://github.com/dotnet/runtime/labels/backlog-cleanup-candidate) label; if this prompts any feedback [the process is undone](https://github.com/dotnet/runtime/issues/7780#event-6400706926), otherwise it gets [closed if no further activity occurs within 14 days](https://github.com/dotnet/runtime/issues/8050#issuecomment-1137995415).

This approach is intended to trigger re-evaluation of older issues both by maintainers and by the community: an issue could get reprioritized or it could be closed as already resolved or obsolete.
