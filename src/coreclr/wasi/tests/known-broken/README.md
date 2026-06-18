# Known-broken WASI smoke scenarios

Tests in this directory currently fail on the WASI CoreCLR corerun and
are deliberately excluded from the default `run-tests.sh` suite to keep
the green baseline clean. They are kept as committed source so the work
to enable them isn't lost.

When a fix lands that makes one of these pass, `git mv` it back into
`../smoke/` and remove the corresponding entry from the list below.

To run a known-broken test directly:

```sh
KEEP_STAGING=1 ./src/coreclr/wasi/tests/run-tests.sh <Name>
# Then inspect the staging dir for the failing wasm trap / stack trace,
# or attach nesm-wasm tools (wasm_run / wasm_terminal_start) directly
# against the staging corerun for full stderr capture and source-level
# DWARF debugging.
```

## Active failures

### `Finalization` — no host event loop to give finalization a turn

Finalizers on WASI are dispatched through `WasiEventLoop`, which only
makes progress when control yields back to a host loop turn. The test
attempts the obvious workaround — make `Main` async and `await
Task.Yield()` between `GC.Collect()` and the assertion — but two
things bite:

1. `async Task<int> Main()` compiles into an entry-point thunk that
   calls `Task.Wait()`. On single-threaded WASI that fails fast in
   `RuntimeFeature.ThrowIfMultithreadingIsNotSupported()`
   (`PlatformNotSupportedException`).
2. Even if the entry point yielded, `Task.Yield()` posts the
   continuation back to the synchronization context / threadpool, and
   there is nothing pumping it in a console-style WASI program — only
   actual `wasi:io/poll` operations drive `WasiEventLoop`.

A test that demonstrates "finalization eventually runs" on WASI
needs to perform a real `wasi:io/poll`-backed await (e.g. a
`Task.Delay`-equivalent that maps to `monotonic-clock.subscribe`) so
the loop actually ticks. Worth doing once the bring-up grows that
plumbing.

