Debugging CoreFX build issues
========================================

## MSBuild debug options

* Enable MSBuild diagnostics log (msbuild.log):
`msbuild my.csproj /flp:v=diag /t:rebuild`
* Generate a flat project file (out.pp):
`msbuild my.csproj /pp:out.pp`

## Steps to debug packaging build issues

(This documentation is work in progress.)

I found the following process to help when investigating some of the build issues caused by incorrect packaging. 

In general, always build the .builds file instead of any of the csproj to ensure that all [Build Pivots](../coding-guidelines/project-guidelines.md#build-pivots) are generated. This applies for running tests as well. For more information, see [Build individual CoreFX DLLs](../project-docs/developer-guide.md#building-individual-corefx-dlls) 

Assuming the current directory is `\src\contractname\`:

1. Build the `\ref` folder: `msbuild /t:rebuild contractname.builds` 


Check the logs for output such as:
```
Project "S:\c1\src\System.Net.ServicePoint\ref\System.Net.ServicePoint.builds" (1) is building "S:\c1\src\System.Net.ServicePoint\ref\System.Net.ServicePoint.csproj" (2:3) on node 1
(Build target(s)).

[...]

CopyFilesToOutputDirectory:
  Copying file from "S:\c1\bin/obj/ref/System.Net.ServicePoint/4.0.0.0/System.Net.ServicePoint.dll" to "S:\c1\bin/ref/System.Net.ServicePoint/4.0.0.0/System.Net.ServicePoint.dll".
  System.Net.ServicePoint -> S:\c1\bin\ref\System.Net.ServicePoint\4.0.0.0\System.Net.ServicePoint.dll
  Copying file from "S:\c1\bin/obj/ref/System.Net.ServicePoint/4.0.0.0/System.Net.ServicePoint.pdb" to "S:\c1\bin/ref/System.Net.ServicePoint/4.0.0.0/System.Net.ServicePoint.pdb".

[...]

Project "S:\c1\src\System.Net.ServicePoint\ref\System.Net.ServicePoint.builds" (1) is building "S:\c1\src\System.Net.ServicePoint\ref\System.Net.ServicePoint.csproj" (2:4) on node 1
(Build target(s)).

[...]

CopyFilesToOutputDirectory:
  Copying file from "S:\c1\bin/obj/ref/System.Net.ServicePoint/4.0.0.0/netcoreapp1.1/System.Net.ServicePoint.dll" to "S:\c1\bin/ref/System.Net.ServicePoint/4.0.0.0/netcoreapp1.1/System.Net.ServicePoint.dll".
  System.Net.ServicePoint -> S:\c1\bin\ref\System.Net.ServicePoint\4.0.0.0\netcoreapp1.1\System.Net.ServicePoint.dll
  Copying file from "S:\c1\bin/obj/ref/System.Net.ServicePoint/4.0.0.0/netcoreapp1.1/System.Net.ServicePoint.pdb" to "S:\c1\bin/ref/System.Net.ServicePoint/4.0.0.0/netcoreapp1.1/System.Net.ServicePoint.pdb".
```

Using your favourite IL disassembler, ensure that each platform contains the correct APIs. Missing APIs from the contracts is likely caused by not having the right `DefineConstants` tags in the csproj files.

2. Build the `\src` folder: `msbuild /t:rebuild contractname.builds`

Use the same technique above to ensure that the binaries include the correct implementations.

3. Build the `\pkg` folder: `msbuild /t:rebuild contractname.builds`

Ensure that all Build Pivots are actually being built. This should build all .\ref and .\src variations as well as actually creating the NuGet packages.

Verify that the contents of the nuspec as well as the actual package is correct. You can find the packages by searching for the following pattern in the msbuild output: 

```
GetPkgProjPackageDependencies:
Skipping target "GetPkgProjPackageDependencies" because it has no inputs.
CreatePackage:
  Created 'S:\c1\bin/packages/Debug/System.Net.Security.4.4.0-beta-24625-0.nupkg'
  Created 'S:\c1\bin/packages/Debug/symbols/System.Net.Security.4.4.0-beta-24625-0.symbols.nupkg'
Build:
  System.Net.Security -> S:\c1\bin/packages/Debug/specs/System.Net.Security.nuspec
```

To validate the content of the nupkg, change the extension to .zip. As before, use an IL disassembler to verify that the right APIs are present within `ref\<platform>\contractname.dll` and the right implementations within the `lib\<platform>\contractname.dll`.

4. Run the tests from `\tests`: `msbuild /t:rebuild,test contractname.Tests.builds`

Ensure that the test is referencing the correct pkg. For example:
```
  <ItemGroup>
    <ProjectReference Include="..\pkg\System.Net.ServicePoint.pkgproj">
      <Name>System.Net.ServicePoint</Name>
      <Project>{53D09AF4-0C13-4197-B8AD-9746F0374E88}</Project>
    </ProjectReference>
  </ItemGroup>
```

Ensure that the right `TargetGroup` (what we're testing) and `TestTFM` (the Target Framework of the test code) are correctly set in the .builds file.

To identify which of the combinations failed, search for the following pattern in the output:

```
Project "S:\c1\src\System.Net.ServicePoint\tests\System.Net.ServicePoint.Tests.builds" (1) is building "S:\c1\src\System.Net.ServicePoint\tests\System.Net.ServicePoint.Tests.csproj"
(2:5) on node 1 (Build target(s)).
ResolvePkgProjReferences:
  Resolved compile assets from .NETStandard,Version=v1.7: S:\c1\bin\ref\System.Net.ServicePoint\4.0.0.0\System.Net.ServicePoint.dll
  Resolved runtime assets from .NETCoreApp,Version=v1.1: S:\c1\bin\AnyOS.AnyCPU.Debug\System.Net.ServicePoint\System.Net.ServicePoint.dll
```

To run a test from a single Build Pivot combination, specify all properties and build the `csproj` instead of the `builds` file:

```
msbuild /t:rebuild,test /p:Outerloop=true "/p:XunitOptions=-showprogress" /p:Configuration=Windows_Debug /p:TargetGroup=netstandard1.7 /p:TestTFM=netcoreapp1.1 .\System.Net.ServicePoint.Tests.csproj
```
Will run the test using the following pivot values:
* Architecture: AnyCPU
* Flavor: Debug
* OS: Windows_NT
* Platform Runtime: netcoreapp1.1
* Target: netstandard1.7
