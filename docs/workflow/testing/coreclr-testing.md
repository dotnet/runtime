# Running .NET Core Tests

Details on test metadata can be found in [test-configuration.md](building/test-configuration.md).

## Build All Tests

1) Build the CoreCLR product
    * [Unix](https://github.com/dotnet/runtime/blob/master/docs/coreclr/building/linux-instructions.md)
    * [OSX](https://github.com/dotnet/runtime/blob/master/docs/coreclr/building/osx-instructions.md)
    * [Windows](https://github.com/dotnet/runtime/blob/master/docs/coreclr/building/windows-instructions.md)
1) From the root directory run the following command:
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

* [Windows](https://github.com/dotnet/runtime/blob/master/docs/coreclr/building/windows-test-instructions.md)
* [Non-Windows](https://github.com/dotnet/runtime/blob/master/docs/coreclr/building/unix-test-instructions.md)
