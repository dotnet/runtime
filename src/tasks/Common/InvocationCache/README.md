# Experimental invocation cache

These sources cache successful MSBuild task invocations and are compiled into each consumer. The API
and storage format are experimental, without compatibility guarantees.

## Integrating a task

Call `InvocationCache.ExecuteCached` with:

- An `InvocationCacheConfiguration` naming the tool and its enable/path environment variables. Only
  `true` enables caching; a path alone does not. Without an override, storage uses the platform's user
  cache directory.
- A description callback returning ordered input paths, effective arguments/context, and an owned
  directory or file set through `InvocationDescription`. Return null for unsupported invocations.
- A normal-execution callback that prepares outputs and runs the tool independently of caching.
- Optional output-capture and replay callbacks. Capture is initialized only on eligible misses,
  before normal execution, and returns a collection of original text lines and their source streams
  (stdout or stderr). The collection must be stable when execution returns. Replay logs each line
  through the task's normal logging path using current policy, not policy cached from execution.

The task must inventory all relevant inputs and toolchain files, including its own assembly. The
shared layer hashes declared files with SHA-256; it does not interpret arguments or discover
dependencies. Inputs must remain stable through discovery, hashing, and execution. Inherited
environment settings and installed runtime trees are not automatically fingerprinted.

## Behavior and limitations

Disabled caching skips discovery and cache I/O. Hits validate and restore outputs without running the
tool. Misses and recoverable cache failures run it normally; only exit code zero with no logged errors
is stored. Failures are diagnosed, and failed tool execution is never retried. Invalid output
declarations, including cache/output overlap, fail before cleanup rather than bypassing caching.

Normal execution owns output preparation, including removal of stale or partially restored files.
The cache owns reconciliation only on hits, including deleting declared files recorded as absent.
Restoration is not transactional. Empty directories are not preserved, and symbolic links are not
checked.

Keys retain absolute input/output paths and execution context; cross-worktree reuse is not promised.
Diagnostics are replayed only when the consumer supplies capture and replay callbacks. Other
non-file task outputs are not restored. Cache operations do not observe cancellation. Consumers must
bypass invocations whose dependencies or required side effects cannot be modeled.

## Stored output and diagnostics

Each entry contains a binary `manifest`, its `manifest.sha256` checksum, an `outputs/` tree, a binary
`diagnostics` file, and a `last-used` timestamp. The manifest records the output contract, file paths,
presence, lengths, and SHA-256 hashes, followed by the diagnostics length and SHA-256 hash. Diagnostics
contain a count followed by each original line and a source-stream byte (0 for stdout, 1 for stderr),
including an explicit zero count for no output. Validate all records and files before modifying
outputs; replay begins only after restoration succeeds.

Logging failures propagate without rerunning the tool, since partial replay cannot be undone. A
replay that logs errors fails the cached invocation. Cold executions that log errors are not stored,
even if the tool exits zero.

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
