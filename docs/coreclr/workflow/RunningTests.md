# Running .NET Core Tests

Details on test metadata can be found in [test-configuration.md](https://github.com/dotnet/coreclr/blob/master/Documentation/building/test-configuration.md).

## Build All Tests

1) Build the CoreCLR product
    * [Unix](https://github.com/dotnet/coreclr/blob/master/Documentation/building/linux-instructions.md)
    * [OSX](https://github.com/dotnet/coreclr/blob/master/Documentation/building/osx-instructions.md)
    * [Windows](https://github.com/dotnet/coreclr/blob/master/Documentation/building/windows-instructions.md)
1) From the root directory run the following command:
    * Non-Windows - `./build-test.sh`
    * Windows - `build-test.cmd`
    * Supply `-h` for usage flags

### Examples

* Build all tests priority `2` and higher
  * `build-test.cmd -priority=2`

## Build Individual Test

Note: The CoreCLR must be built prior to building an individual test. See first step for building all tests.

* Native Test: Build the generated CMake projects
  * Projects are auto-generated when the `build-test.sh`/`build-test.cmd` script is run
* Managed Test: Invoke MSBuild on the project directly
  * Non-Windows - All of the necessary tools to build are under `coreclr/Tools`. It is possible to use `coreclr/Tools/MSBuild.dll` as you would normally use MSBuild with a few caveats. The `coreclr/Tools/msbuild.sh` script exists to make the call shorter.
    * **Note:** Passing `/p:__BuildOs=`[`OSX`|`Linux`] is required.
  * Windows - Use Visual Studio Developer command prompt

### Examples

* Using the `dotnet.sh` script
  * `coreclr/dotnet.sh msbuild /maxcpucount coreclr/tests/src/JIT/CodeGenBringUpTests/Array1.csproj /p:__BuildType=Release /p:__BuildOS=OSX`

## Additional Documents

* [Windows](https://github.com/dotnet/coreclr/blob/master/Documentation/building/windows-test-instructions.md)
* [Non-Windows](https://github.com/dotnet/coreclr/blob/master/Documentation/building/unix-test-instructions.md)
