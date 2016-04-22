Testing with CoreFX
===================

It may be valuable to use CoreFX tests to validate your changes to CoreCLR or mscorlib.

**Windows**

As part of building tests, CoreFX restores a copy of the runtime from myget, in order to update the runtime that is deployed, a special build property `BUILDTOOLS_OVERRIDE_RUNTIME` can be used. If this is set, the CoreFX testing targets will copy all the files in the folder it points to into the test folder, overwriting any files that exist.

To run tests, follow the procedure for [running tests in CoreFX](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md). You can pass `/p:BUILDTOOLS_OVERRIDE_RUNTIME=<path-to-coreclr>\bin\Product\Windows_NT.x64.Release` to build.cmd to set this property.

**FreeBSD, Linux, NetBSD, OS X**

Refer to the procedure for [running tests in CoreFX](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-platform-testing.md)
- Note the --coreclr-bins and --mscorlib-bins arguments to [run-test.sh](https://github.com/dotnet/corefx/blob/master/run-test.sh)
- Pass in paths to your private build of CoreCLR
