# Building

To build just CoreCLR, use the `--subsetCategory` flag to the `build.sh` (or `build.cmd`) at the repo root:

```
./build.sh --subsetCategory coreclr
```
or on Windows,
```
build.cmd --subsetCategory coreclr
```

By default, build generates a 'debug' build type, that includes asserts and is easier for some people to debug. If you want to make performance measurements, or just want tests to execute more quickly, you can also build the 'release' version which does not have these checks by adding the flag `-configuration release` (or `-c release`), for example
```
./build.sh --subsetCategory coreclr -configuration release
```

This will produce outputs as follows:

- Product binaries will be dropped in `artifacts\bin\coreclr\<OS>.<arch>.<flavor>` folder.
- A NuGet package, Microsoft.Dotnet.CoreCLR, will be created under `artifacts\bin\coreclr\<OS>.<arch>.<flavor>\.nuget` folder.
- Test binaries will be dropped under `artifacts\tests\coreclr\<OS>.<arch>.<flavor>` folder.


The build places logs in `artifacts\log` and these are useful when the build fails.

The build places all of its output in the `artifacts\obj\coreclr` directory, so if you remove that directory you can force a
full rebuild.

The build has a number of options that you can learn about using `build -?`. In particular  `-skiptests` skips building the tests, which makes the build quicker.

See [Running Tests](../../testing/coreclr/testing.md) for instructions on running the tests.
