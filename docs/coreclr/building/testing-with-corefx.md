Testing with CoreFX
===================

It may be valuable to use CoreFX tests to validate your changes to CoreCLR or mscorlib.

**Windows**

In order to do this you need to create a file called `localpublish.props` under the `<repo root>\packages` folder.
The contents of the file should look like this (make sure to update the version to the current version of the CoreCLR package used by CoreFx):

```xml
<Project ToolsVersion="12.0" DefaultTargets="Build" 
     xmlns="http://schemas.microsoft.com/developer/msbuilding/2003">
  <ItemGroup>
  <LocalPackages Include="$(PackagesBinDir)">
      <PackageName>Microsoft.DotNet.CoreCLR</PackageName>
      <PackageVersion>1.0.2-prerelease</PackageVersion>
      <InstallLocation><corefx repo root>\packages</InstallLocation>
    </LocalPackages>
  </ItemGroup>
</Project>
```

Once this file is there, subsequent builds of the CoreCLR repo will install the CoreCLR package into the location specified by `InstallLocation`.

To run tests, follow the procedure for [running tests in CoreFX](https://github.com/dotnet/corefx/blob/master/Documentation/building/windows-instructions.md).

**Linux, OS X, FreeBSD**

Refer to the procedure for [running tests in CoreFX](https://github.com/dotnet/corefx/blob/master/Documentation/building/cross-platform-testing.md)
- Note the --coreclr-bins and --mscorlib-bins arguments to [run-test.sh](https://github.com/dotnet/corefx/blob/master/run-test.sh)
- Pass in paths to your private build of CoreCLR
