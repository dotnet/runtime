## WasmBasicTestApp

This is a test application used by various Wasm.Build.Tests. The idea is to share a common behavior (so that we don't have to maintain many test apps) and tweak it for the test case.
It typically suits scenario where you need more than a plain template app. If the test case is too different, feel free to create another app.

### Usage

The app reads `test` query parameter and uses it to switch between test cases. Entrypoint is `main.js`.
There is common unit, then switch based on test case for modifying app startup, then app starts and executes next switch based on test case for actually running code.

Some test cases passes additional parameters to differentiate behavior, see `src/mono/wasm/Wasm.Build.Tests/TestAppScenarios`.

### Running out side of WBT

One of the benefits is that you can copy the app out of intree and run the app without running Wasm.Build.Tests with just `dotnet run`.