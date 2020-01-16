# Running .NET Core Tests

Details on test metadata can be found in [test-configuration.md](test-configuration.md).

## Build All Tests

1) Build the CoreCLR product
    * [Unix](../../building/coreclr/linux-instructions.md)
    * [OSX](../../building/coreclr/osx-instructions.md)
    * [Windows](../../building/coreclr/README.md)
1) [Build the libraries](../../building/libraries/README.md) in Release configuration. Pass the configuration of CoreCLR you just built to the build script (e.g. `/p:CoreCLRConfiguration=Debug`).
1) From the src/coreclr directory run the following command:
    * Non-Windows - `./build-test.sh`
    * Windows - `build-test.cmd`
    * Supply `-h` for usage flags

### Test priority

The CoreCLR tests have two priorities 0 and 1, the priority 0 tests run by default on all PRs, while the priority 1 tests run out outerloop CI runs.

### Examples

* Build all tests priority `1` and higher
  * `build-test.cmd -priority=1`
  * `build-test.sh -priority1`

## Build Individual Test

Note:  CoreCLR must be built prior to building an individual test. See first step for building all tests.

* Native Test: Build the generated CMake projects
  * Projects are auto-generated when the `build-test.sh`/`build-test.cmd` script is run
    * It is possible to explicitely run only the native test build with `build-test.sh/cmd skipmanaged`
* Managed Test: Invoke dotnet msbuild on the project directly
  > ~/runtime/.dotnet/dotnet msbuild ~/runtime/src/coreclr/tests/src/JIT/CodegenBringupTests/Array1.csproj /p:__BuildOs=OSX /p:__BuildType=Release
  - Note that /p:__BuildOs=`[Windows_NT, OSX, Linux]` is required.

## Additional Documents

* [Windows](../../testing/coreclr/windows-test-instructions.md)
* [Non-Windows](../../testing/coreclr/unix-test-instructions.md)
