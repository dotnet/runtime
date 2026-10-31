## WasmBasicTestApp

This is a test application used by various Wasm.Build.Tests. The idea is to share a common behavior (so that we don't have to maintain many test apps) and tweak it for the test case.
It typically suits scenario where you need more than a plain template app. If the test case is too different, feel free to create another app.

### Usage

The app reads `test` query parameter and uses it to switch between test cases. Entrypoint is `main.js`.
There is common unit, then switch based on test case for modifying app startup, then app starts and executes next switch based on test case for actually running code.

Some test cases passes additional parameters to differentiate behavior, see `src/mono/wasm/Wasm.Build.Tests`.

For CoreCLR runs, Wasm.Build.Tests enables a shared startup download queue guard in `main.js`.
The loader's internal `onStartupDownloadQueueComputed` hook establishes the total after configuration
initializers finish. Progress must report that same total until startup completes, and all counted
resources must finish. `download()` and the subsequent `create()` are checked separately, including
an empty `create()` phase. HTTP-cache-only prefetch does not use this accounting; its subsequent
`create()` is checked. The guard stops before test execution so lazy loading may add resources.
Mono runs, build-only tests, and tests replacing the shared entry point do not use this guard.

### Running out side of WBT

One of the benefits is that you can copy the app out of intree and run the app without running Wasm.Build.Tests with just `dotnet run`.