# Wasm debugger

## Debug proxy

- Protocol messages are truncated when logged, to 64k, by default. But this can be changed by setting `WASM_DONT_TRUNCATE_LOG_MESSAGES=1`.

## Projects

- `DebuggerTestSuite` - project with all the tests, and the test harness
- `Wasm.Debugger.Tests` - a wrapper project to fit in the global build
