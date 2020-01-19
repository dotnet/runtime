# Building Mono

To build just Mono runtime, use the `--subsetCategory` flag to the `build.sh` (or `build.cmd`) at the repo root:

```bash
./build.sh --subsetCategory mono
```
or on Windows,
```bat
build.cmd --subsetCategory mono
```

By default, build generates a 'debug' build output, that includes asserts, less code optimizations and is easier for debugging. If you want to make performance measurements, or just want tests to execute more quickly, you can also build the 'release' version which does not have these checks by adding the flag `-configuration Release` (or `-c Release`) and `/p:__BuildType=Release`, for example
```bash
./build.sh --subsetCategory mono -configuration Release /p:__BuildType=Release
```

Product binaries will be dropped in `artifacts\bin\mono\<OS>.<arch>.<flavor>` folder.

To generate nuget packages:

```bash
./build.sh --subsetCategory mono -pack (with optional release configuration)
```
or on Windows,
```bat
build.cmd --subsetCategory mono -pack (with optional release configuration)
```

The following packages will be created under `artifacts\packages\<configuration>\Shipping`:

- `Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `runtime.<OS>.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `transport.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`
- `transport.runtime.<OS>.Microsoft.NETCore.Runtime.Mono.<version>-dev.<number>.1.nupkg`

Test binaries are not yet available for mono.

The build places logs in `artifacts\log` and these are useful when the build fails.

The build places all of its output in the `artifacts\obj\mono` directory, so if you remove that directory you can force a
full rebuild.

The build has a number of options that you can learn about using `build -?`. 