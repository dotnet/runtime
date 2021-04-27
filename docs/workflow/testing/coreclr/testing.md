# Running Tests

Details on test metadata can be found in [test-configuration.md](test-configuration.md).

## Build All Tests

1) Build the CoreCLR product
    * [Unix](../../building/coreclr/linux-instructions.md)
    * [macOS](../../building/coreclr/osx-instructions.md)
    * [Windows](../../building/coreclr/README.md)
2) [Build the libraries](../../building/libraries/README.md) in Release configuration. Pass the configuration of CoreCLR you just built to the build script (e.g. `-runtimeconfiguration debug`).
3) From the root directory run the following command:
    * Non-Windows - `src/tests/build.sh`
    * Windows - `src\tests\build.cmd`
    * Supply `-h` for usage flags

### Test priority

The CoreCLR tests have two priorities, 0 and 1. The priority 0 tests run by default on all pull requests (PRs), while the priority 1 tests run in outerloop CI runs.

### Examples

* Build all tests priority 1 and higher
  * `build.cmd -priority=1`
  * `build.sh -priority1`

## Build Individual Test

Note:  CoreCLR must be built prior to building an individual test. See the first step, above, for building all tests.

* Native Test: Build the generated CMake projects
  * Projects are auto-generated when the `build.sh`/`build.cmd` script is run
    * It is possible to explicitly run only the native test build with `build.sh/cmd skipmanaged`
* Managed Test: Invoke `dotnet build` on the project directly. `dotnet` can be the `dotnet.sh` or `dotnet.cmd` script in the repo root.
  ```
  <runtime-repo-root>/dotnet.sh build <runtime-repo-root>/src/tests/<path-to-project> -c <configuration>
  ```
  * To build managed test projects with dependencies on native test projects, the native test project must first be built. The managed test should then be built using `dotnet build --no-restore` to skip restoring.

## Additional Documents

* [Windows](windows-test-instructions.md)
* [Non-Windows](unix-test-instructions.md)
