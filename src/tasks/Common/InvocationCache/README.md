# Experimental invocation cache

These sources cache successful MSBuild task invocations and are compiled into each consumer. The API
and storage format are experimental, without compatibility guarantees.

## Integrating a task

Derive from `CachedToolTask` and compile the shared sources into the task assembly. Override:

- `CacheConfiguration` with an `InvocationCacheConfiguration` naming the tool and its enable/path
  environment variables. Only `true` enables caching; a path alone does not. Without an override,
  storage uses the platform's user cache directory.
- `DescribeInvocation` to return ordered input paths, effective arguments/context, and an owned
  directory or file set through `InvocationDescription`. Return null for unsupported invocations.
- `ExecuteToolCore` when the task needs tool-specific output preparation before calling the base
  implementation. It runs only on misses and bypasses.

The base class calls `InvocationCache.TryCreateFromEnvironment` first. Only if a cache is available
does it call `DescribeInvocation`, passing a supported description to that cache's
instance `PrepareInvocation` method. Preparation owns output validation, timestamping, hashing, and
restoration.
On an eligible miss, the cache returns a `PendingInvocation` owning separate ordered stdout and stderr
lists. The base class captures received lines into those buffers instead of logging them, then calls
`Complete` with the tool exit code. Completion returns an `InvocationResult` using the same buffers and
stores successful execution. Per-stream output must be deterministic for the same key.

On a hit, preparation returns no pending invocation and its `restoredResult` output contains the restored
result. On a bypass, both are null; the base class executes normally without capture. Only eligible
misses allocate capture buffers, and pending capture state is released even when execution throws.

`CachedToolTask` replays returned output on both hits and misses through its ordinary `ToolTask`
logging path, using current importance and stderr-as-error policy rather than policy cached from
execution. These settings are not passed to the cache. Cacheable executions defer tool-output logging
until execution completes; bypasses keep normal live logging. Partial captured output is also replayed
when execution throws. Capture must remain pre-policy: store raw lines, not already promoted or
suppressed MSBuild events.

Replay receives only the stdout/stderr sequences and returns nothing. The cache result is unchanged;
`CachedToolTask` checks logged errors when determining the final task exit code.

The task must inventory all relevant inputs and toolchain files, including its own assembly. The
shared layer hashes declared files with SHA-256; it does not interpret arguments or discover
dependencies. Inputs must remain stable through discovery, hashing, and execution. Inherited
environment settings and installed runtime trees are not automatically fingerprinted.

## Behavior and limitations

Disabled caching skips discovery and cache I/O. Hits validate and restore outputs without running the
tool. Misses and recoverable cache failures run it normally; only tool exit code zero is stored,
independently of logged errors. Failures are diagnosed, and failed tool execution is never retried.
Invalid output declarations, including cache/output overlap, fail before cleanup rather than bypassing
caching.

Normal execution owns output preparation, including removal of stale or partially restored files.
The cache owns reconciliation only on hits, including deleting declared files recorded as absent.
Restoration is not transactional. Empty directories are not preserved, and symbolic links are not
checked.

Keys retain absolute input/output paths and execution context; cross-worktree reuse is not promised.
Diagnostics are restored from the line buffers filled during execution. Other non-file task outputs
are not restored. Cache operations do not observe cancellation. Consumers must
bypass invocations whose dependencies or required side effects cannot be modeled.

## Stored output and diagnostics

Each entry contains a binary `manifest`, its `manifest.sha256` checksum, an `outputs/` tree, a binary
`diagnostics` file, and a `last-used` timestamp. The manifest records the output contract, file paths,
presence, lengths, and SHA-256 hashes, followed by the diagnostics length and SHA-256 hash. Diagnostics
contain two separate sequences, stdout followed by stderr, each with a count and its original lines.
Empty streams have an explicit zero count. Lines are replayed in the same fixed stream order,
preserving line order within each stream. Cross-stream callback timing
does not affect the stored diagnostics or their integrity records; bypassed logging retains the
tool task's ordinary interleaving. Validate all records and files before modifying outputs; replay
on hits begins only after restoration succeeds.

The cache returns diagnostic data without replaying it. `CachedToolTask` owns replay after restoration
or execution completes. Logging failures must not rerun the tool, since partial replay cannot be undone.
Replay that logs errors fails the task. Successful tool executions are stored even when current logging
policy fails the task; subsequent hits reapply that policy to the captured output.

The experimental format remains version 1. Consumers include their task assembly contents in the
key, invalidating entries when these compiled-in sources change. Entries missing diagnostics or
their integrity record fail validation rather than being assumed warning-free.

Entries are staged locally and published atomically. Concurrent processes on one machine may share
storage when writing distinct destinations; same-destination execution and distributed writers are
unsupported.

There is no automatic eviction. Corrupt or incompatible entries are not repaired; obsolete entries
within the current `v1` layout remain purgable. Purge or manual deletion must not race active users.

See the [ILLink integration](../../../../docs/design/tools/illink/task-cache.md) for eligibility and
toolchain omissions, and the [maintenance tool README](../../../tools/BuildCache/README.md) for purge
behavior and commands.
