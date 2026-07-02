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

_(none currently — the previous `Finalization` scenario now passes and
has been promoted to `../smoke/Finalization/` after the AsyncHelpers.Wasi
+ wasi:clocks + WasiEventLoop pieces of this bring-up landed.)_
