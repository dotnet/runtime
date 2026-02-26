# Incremental Build Frustrations in dotnet/runtime

Issues encountered while testing a crossgen2 + coreclr change in the test infrastructure.

## 1. `_CreateR2RImages` doesn't track crossgen2 binary changes

**Problem**: When rebuilding crossgen2 via `./build.sh clr.tools`, the test build's `_CreateR2RImages` target considers R2R outputs "up-to-date" and skips re-crossgen2'ing the test DLLs. The MSBuild target only checks the input DLLs, not the crossgen2 tool itself.

**Impact**: Test ran with stale R2R images from an old crossgen2, silently. No warning that crossgen2 changed. Required manually deleting `artifacts/obj/` and `artifacts/bin/` for the test project to force a full rebuild.

**Repro**: `./build.sh clr.tools -c Release` then re-run `dotnet build <test>.csproj /p:testreadytorun=true` — R2R images are not regenerated.

**Suggested fix**: The `_CreateR2RImages` target should include the crossgen2 binary (and its managed dependencies like `ILCompiler.ReadyToRun.dll`) as inputs so MSBuild detects changes.

## 2. `./build.sh clr.runtime` doesn't update testhost

**Problem**: After rebuilding `libcoreclr.so` via `./build.sh clr.runtime`, the testhost at `artifacts/bin/testhost/net11.0-linux-Release-x64/shared/Microsoft.NETCore.App/11.0.0/` is NOT updated. The test publish step copies from testhost, so tests run with the old runtime.

**Impact**: After fixing a bug in coreclr VM code, tests still used the old crashing runtime. Had to manually `cp` the new `libcoreclr.so` to testhost.

**Repro**: Modify `src/coreclr/vm/method.cpp`, run `./build.sh clr.runtime -c Release`, then run tests — testhost still has old `libcoreclr.so` (verified via `md5sum`).

**Suggested fix**: Either `clr.runtime` should update the testhost, or there should be a lightweight `clr.testhost` or `libs.pretest` target that does this without rebuilding all of libs.

## 3. `rm -rf artifacts/bin/<TestProject>/` is not enough to force R2R rebuild

**Problem**: Even after deleting the test bin directory (`artifacts/bin/System.Threading.Tasks.Parallel.Tests/`), the R2R crossgen2 step still says "up-to-date" because the R2R intermediate outputs live in `artifacts/obj/<TestProject>/Release/net11.0/R2R/`.

**Impact**: Had to discover and also delete `artifacts/obj/` to get a true clean rebuild.

**Suggested fix**: Cleaning should be easier. Either `dotnet build -t:Clean` should handle this, or the R2R outputs should live alongside the bin outputs so a single `rm -rf` cleans everything.

## 4. Multiple crossgen2 output directories are confusing

**Problem**: There are at least 3 crossgen2 output locations:
- `artifacts/bin/coreclr/linux.x64.Release/crossgen2/` (managed DLLs)
- `artifacts/bin/coreclr/linux.x64.Release/x64/crossgen2/` (native host + PDBs, used by tests)
- `artifacts/bin/coreclr/linux.x64.Release/crossgen2-published/` (published single-file)

The test build uses `x64/crossgen2/crossgen2` (the native host). It was unclear which directory has the authoritative build and which is actually used.

**Impact**: Time spent comparing MD5 hashes across directories to figure out if the right crossgen2 was being used.

**Suggested fix**: Document which crossgen2 directory is used by the test infrastructure, or consolidate to fewer output directories.

## 5. Manual `cp` of libcoreclr to testhost gets silently overwritten (or not)

**Problem**: After manually copying `libcoreclr.so` to the testhost to test a VM fix, subsequent `dotnet build` of the test project may or may not overwrite it depending on whether the publish step considers it up-to-date. This creates an unpredictable state.

**Impact**: At one point the testhost had the old (method.cpp fixed) libcoreclr that I'd manually copied, and the build didn't replace it, so I was unknowingly testing with two fixes active instead of one.

**Suggested fix**: There should be a single command like `./build.sh libs.pretest -c Release` that refreshes the testhost from build outputs, and test builds should always use that testhost. Or the test infrastructure should detect when the testhost is stale.
