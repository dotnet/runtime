# Building

Once all the necessary tools are in place, building is trivial. Simply run coreclr.cmd/sh script that lives in the repository root.

```bat
    .\coreclr.cmd

    [Lots of build spew]

    Product binaries are available at C:\git\runtime\artifacts\bin\coreclr\Windows_NT.x64.debug
    Test binaries are available at C:\git\runtime\artifacts\tests\coreclr\Windows_NT.x64.debug
```

As shown above, the product will be placed in

- Product binaries will be dropped in `artifacts\bin\coreclr\<OS>.<arch>.<flavor>` folder.
- A NuGet package, Microsoft.Dotnet.CoreCLR, will be created under `artifacts\bin\coreclr\<OS>.<arch>.<flavor>\.nuget` folder.
- Test binaries will be dropped under `artifacts\tests\coreclr\<OS>.<arch>.<flavor>` folder.

By default, build generates a 'Debug' build type, that has extra checking (assert) compiled into it. You can
also build the 'release' version which does not have these checks.

The build places logs in `artifacts\log` and these are useful when the build fails.

The build places all of its output in the `artifacts\obj\coreclr` directory, so if you remove that directory you can force a
full rebuild.

The build has a number of options that you can learn about using build -?.   Some of the more important options are

 * -skiptests - don't build the tests.   This can shorten build times quite a bit, but means you can't run tests.
 * -release - build the 'Release' build type that does not have extra development-time checking compiled in.
 You want this if you are going to do performance testing on your build.

See [Running Tests](../../testing/coreclr-testing.md) for instructions on running the tests.
