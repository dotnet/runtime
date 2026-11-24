# Libraries

Covers `src/libraries/`, including Browser/WASM, WASI, mobile, Windows, and Unix targets. Confirm the component's [baseline sentinel](../SKILL.md#baseline-sentinels) (`artifacts/bin/testhost/`) before running tests; if missing, start `./build.sh clr+libs -rc release` once in the background immediately and code in parallel (editing `tests/**` directly in-tree and staging `src/**` / `ref/**` edits under `artifacts/staged/<repo-rel-path>`) rather than waiting idle.

## Parallel Coding While Step 1 Runs

While `./build.sh clr+libs -rc release` runs (~9–12 min), do **not** poll idle and do **not** touch `src/` or `ref/` in-tree:

1. Read `<NetCoreAppCurrent>` in `eng/Versions.props`, `<TargetFramework>` / `<TargetFrameworks>` in `src/libraries/<Lib>/src/<Lib>.csproj`, matching domain `references/*.md`, callers, sibling PAL files, and `ref/<Lib>.cs`.
2. Edit or add tests directly under `src/libraries/<Lib>/tests/**` (`clr+libs` does not build `libs.tests`).
3. Stage product/contract edits under `artifacts/staged/<repo-rel-path>`:
   ```bash
   mkdir -p artifacts/staged/$(dirname src/libraries/<Lib>/src/<File>.cs)
   cp src/libraries/<Lib>/src/<File>.cs artifacts/staged/src/libraries/<Lib>/src/<File>.cs
   ```
   Edit the staged files under `artifacts/staged/...` (never start another `dotnet`, `./dotnet.sh`, `msbuild`, or `./build.sh` while Step 1 is running).
4. Once your staged edits and tests are ready, wait for `./build.sh` to exit `0`, then promote and delete `artifacts/staged/` before building incrementally:
   ```bash
   cp -R artifacts/staged/* . && rm -rf artifacts/staged
   ```

## Fast Inner Loop (Critical: Keep `testhost` Updated in ~5–15s)

Test `.csproj` projects reference `ref/<Lib>.csproj` — **not** `src/<Lib>.csproj` — and execute against `artifacts/bin/testhost/<tfm>-<os>-Release-<arch>/`. Running `/t:Test` on a test project alone compiles the test DLL against `ref/` and runs against **stale** product binaries in `testhost`!

Follow this exact sequence from the repository root using `./dotnet.sh` (`.\dotnet.cmd` on Windows) and **never run two `dotnet build` commands concurrently** (concurrent builds collide on shared CoreLib/ILLink outputs with `IL1011` file locks):

1. **If public API (`ref/`) changed, build `ref` first** (~3s; see `ref-assemblies.instructions.md`):
   ```bash
   ./dotnet.sh build --no-restore src/libraries/<Lib>/ref/<Lib>.csproj /p:RuntimeConfiguration=Release
   ```
2. **During iterative coding, build `src` with `-f <host-tfm>` (~4–8s) to binplace into `testhost`:**
   Read `<TargetFramework>` / `<TargetFrameworks>` in `src/libraries/<Lib>/src/<Lib>.csproj` (using `<tfm>` = `<NetCoreAppCurrent>` from `eng/Versions.props`, e.g. `net11.0` or `net12.0`) and pick the **most specific entry actually listed** for the current host OS:
   - **macOS:** `<tfm>-osx` if listed → else `<tfm>-unix` if listed → else `<tfm>` (omit `-f` if singular `<TargetFramework>`).
   - **Linux:** `<tfm>-linux` if listed → else `<tfm>-unix` if listed → else `<tfm>`.
   - **Windows:** `<tfm>-windows` if listed → else `<tfm>`.
   - *Examples on macOS:* `System.Net.Http`, `System.Net.Sockets`, `System.Net.Security` → `-f <tfm>-osx`; `System.IO.Compression`, `System.Formats.Tar` → `-f <tfm>-unix`; `System.Private.Uri`, `System.IO.Packaging` → `-f <tfm>` (or omit `-f` if singular `<TargetFramework>`).
   ```bash
   ./dotnet.sh build --no-restore src/libraries/<Lib>/src/<Lib>.csproj /p:RuntimeConfiguration=Release -f <host-tfm>
   ```
3. **Right before the author review gate (and after any post-review edit), verify all target frameworks WITHOUT `-f`:**
   ```bash
   ./dotnet.sh build --no-restore src/libraries/<Lib>/src/<Lib>.csproj /p:RuntimeConfiguration=Release
   ```
   Building `src/<Lib>.csproj` without `-f` compiles all configured target frameworks (`windows`, `unix`, `osx`, `browser`, `wasi`, `android`, `ios`) against reference assemblies in seconds — catching `CA1416` (`[UnsupportedOSPlatform]`/`[SupportedOSPlatform]`), `#if` signature drift, trimming warnings, and `ApiCompat` mismatches without needing WASM/mobile workloads installed. If `<TestProject>.csproj` also multi-targets additional frameworks (such as `$(NetFrameworkCurrent)` or `netstandard*`), run `./dotnet.sh build --no-restore src/libraries/<Lib>/tests/<TestProject>.csproj /p:RuntimeConfiguration=Release` once (without `-f` or `/t:Test`, ~3s) after restoring assets so downlevel compile breaks (`CS1069`) fail before review even though `/t:Test` on Unix/macOS only builds `$(NetCoreAppCurrent)`.
4. **If `src/native/libs/**` C/C++ shims changed, rebuild `libs.native` AND copy into `testhost`:**
   ```bash
   ./build.sh libs.native -rc release
   ```
   `libs.native` writes `.dylib`/`.so`/`.dll` under `artifacts/bin/native/<tfm>-<os>-Release-<arch>/` and does **not** overwrite an already-populated `testhost`. Copy the rebuilt native binary into `artifacts/bin/testhost/<tfm>-<os>-Debug-<arch>/shared/Microsoft.NETCore.App/*/` (or run `./build.sh libs.pretest -rc release`) before running managed tests.
5. **Run targeted, filtered xUnit tests (omit `--no-restore` on the FIRST `/t:Test` of each test `.csproj`, pass `--no-restore` on 2nd+ runs):**
   `./build.sh clr+libs` does **not** restore `libs.tests`, so passing `--no-restore` on the first `/t:Test` of a test project fails with `MSB4057: The target "GetProjectWithBestTargetFrameworks" does not exist`. Omit `--no-restore` once per test `.csproj`, then pass `--no-restore` on every subsequent `/t:Test` of that `.csproj`:
   ```bash
   # FIRST /t:Test run on a test .csproj (restores test project assets once):
   ./dotnet.sh build src/libraries/<Lib>/tests/<TestProject>.csproj /t:Test /p:RuntimeConfiguration=Release /p:XUnitClassName=Exact.Namespace.ClassName

   # SUBSEQUENT /t:Test runs on that same test .csproj (add --no-restore; '*' wildcard works on XUnitMethodName):
   ./dotnet.sh build --no-restore src/libraries/<Lib>/tests/<TestProject>.csproj /t:Test /p:RuntimeConfiguration=Release "/p:XUnitMethodName=Exact.Namespace.ClassName.MethodPrefix*"

   # Multiple classes or methods in one invocation (NEVER pass ';' or ',' in XUnitClassName/XUnitMethodName — MSBuild fails with MSB1006):
   ./dotnet.sh build --no-restore src/libraries/<Lib>/tests/<TestProject>.csproj /t:Test /p:RuntimeConfiguration=Release "/p:XUnitOptions=-class Ns.ClassA -class Ns.ClassB"
   ```
   - Re-running the same built test binary with a different filter can add `/p:TestNoBuild=true`.
   - Before adding tests, inspect `<TargetFrameworks>` and conditional `<Compile>` groups (`*.Net.cs` vs shared `*.cs`) in `<TestProject>.csproj` so `.NETCoreApp`-only APIs are placed in the matching file or guarded with `#if NET`.
   - Always check the xUnit summary: **`Total: 0` or `Skipped == Total` means nothing ran** (check exact namespace — e.g. `System.PrivateUri.Tests`, not `System.Private.Uri.Tests` — and remember `XUnitClassName` does not expand `*`).
   - **Never run unfiltered full assemblies** for `System.Net.Http.Functional.Tests` (`25–45m`), `System.Net.Sockets.Tests` (unfiltered socket tests can hang on macOS), `System.Net.Security.*Tests` (external OCSP/Kerberos endpoints), or `System.IO.Compression.ZipFile.Tests` (multi-GiB Zip64 cases). Filter to the affected test classes plus directly related sibling classes.
   - **Keep test timers short:** When writing loopback/socket/keep-alive/timeout tests, configure delays/intervals to `100–500ms` and wrap awaits with `.WaitAsync(TimeSpan.FromSeconds(5))` so failing reproductions fail in `<5s` rather than hanging `100–180s` on `HttpClient.Timeout` or `TestSettings.PassingTestTimeout`.

**System.Private.CoreLib:** rebuild with `./build.sh clr.corelib+clr.nativecorelib+libs.pretest -rc checked`

## Reproducing a library bug

Before choosing the fix's scope, follow the affected data through its producers,
consumers, and shared validators; when relaxing or tightening a format or validation
rule, update the coupled read-side parsers and write-side validators/serializers together
before the post-fix test run. When the root cause or API contract is already clear while
Step 1 runs, write the regression test in `tests/**` (reading what existing test helpers
actually produce and assert before relying on them) and stage the fix under
`artifacts/staged/<repo-rel-path>`, then promote the staged fix as soon as Step 1 exits `0`,
build `-f <host-tfm>`, and verify your filtered tests pass (`Total > 0, Failed: 0`). Run an
unfixed filtered repro first (bounded with short timeouts) when needed to diagnose an
uncertain root cause. Run tests with the repo SDK (`./dotnet.sh`) via
[single-library testing](/docs/workflow/testing/libraries/testing.md#running-tests-for-a-single-library).
Use a standalone repro only if tests can't express the scenario; it must use the
locally built library and testhost, with frameworks and paths read from the
evaluated project, not hardcoded. Check the loaded assembly's `Location` and
identity before trusting results. Treat SDK/reference mismatches as setup errors
and return to the test project. `standalone-test-repro` covers `src/tests` only.

## WASM / WASI Runtime Execution (only when running tests on the WASM host)

Cross-platform compilation and `CA1416` verification for `browser`/`wasi` are already covered by Step 3 (`./dotnet.sh build --no-restore src/libraries/<Lib>/src/<Lib>.csproj /p:RuntimeConfiguration=Release` without `-f`). Run `./build.sh libs -os browser` / `./build.sh libs.tests -test -os browser` only when executing tests under the WASM runtime with EMSDK/WASM workloads installed.

## Reference

- [Build Libraries](/docs/workflow/building/libraries/README.md) · [Test Libraries](/docs/workflow/testing/libraries/testing.md)
- [WASM Build](/docs/workflow/building/libraries/webassembly-instructions.md) · [WASM Test](/docs/workflow/testing/libraries/testing-wasm.md)
