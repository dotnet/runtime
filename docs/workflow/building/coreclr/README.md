# Building

These are instructions for building the common form of CoreCLR on common platforms. For instructions on particular form factors, see also [Build NativeAOT](nativeaot.md), [Build CoreCLR on Linux](linux-instructions.md), [Build CoreCLR on OS X](osx-instructions.md), [Build CoreCLR on FreeBSD](freebsd-instructions.md),
[Cross Compilation for ARM on Windows](cross-building.md), [Cross Compilation for Android on Linux](android.md).

To build just CoreCLR, use the `-subset` flag to the `build.sh` (or `build.cmd`) script at the repo root:

For Linux:
```
./build.sh -subset clr
```

For Windows:
```
build.cmd -subset clr
```
Specifying `-subset` explicitly is not necessary if it is the first argument: `./build -subset clr` and `./build clr` are equivalent.

By default, build generates a 'debug' build type, that includes asserts and is easier for some people to debug. If you want to make performance measurements, or just want tests to execute more quickly, you can also build the 'release' version (which does not have these checks) by adding the flag `-configuration release` (or `-c release`), for example:
```
./build.sh -subset clr -configuration release
```

CoreCLR also supports a 'checked' build type which has asserts enabled like 'debug', but is built with the native compiler optimizer enabled, so it runs much faster. This is the usual mode used for running tests in the CI system. You can build that using, for example:
```
./build.sh -subset clr -configuration checked
```

If you want to use Ninja to drive the native build instead of Make on non-Windows platforms, you can pass the `-ninja` flag to the build script as follows:
```
./build.cmd -subset clr -ninja
```

If you want to use Visual Studio's MSBuild to drive the native build on Windows, you can pass the `-msbuild` flag to the build script similarly to the `-ninja` flag.

We recommend using Ninja for building the project on Windows since it more efficiently uses the build machine's resources for the native runtime build in comparison to Visual Studio's MSBuild.

To pass extra compiler/linker flags to the coreclr build, set the environment variables `EXTRA_CFLAGS`, `EXTRA_CXXFLAGS` and `EXTRA_LDFLAGS` as needed. Don't set `CFLAGS`/`CXXFLAGS`/`LDFLAGS` directly as that might lead to configure-time tests failing.

This will produce outputs as follows:

- Product binaries will be dropped in `artifacts\bin\coreclr\<OS>.<arch>.<flavor>` folder.
- A NuGet package, Microsoft.Dotnet.CoreCLR, will be created under `artifacts\bin\coreclr\<OS>.<arch>.<flavor>\.nuget` folder.
- Test binaries will be dropped under `artifacts\tests\coreclr\<OS>.<arch>.<flavor>` folder. However, the root build script will not build the tests.

The build places logs in `artifacts\log` and these are useful when the build fails.

The build places all of its intermediate output in the `artifacts\obj\coreclr` directory, so if you remove that directory you can force a
full rebuild.

To build CoreCLR, the root build script invokes the `src\coreclr\build.cmd` (or build.sh) script. To build the CoreCLR tests, you must use this script.
Use `build -?` to learn about the options to this script.

See [Running Tests](../../testing/coreclr/testing.md) for instructions on running the tests.


