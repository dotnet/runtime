[Consumes API specification](https://github.com/dotnet/buildtools/blob/master/Documentation/RepoCompose.md#consumes-1)

In CoreFx, we'll be using the RepoUtil tool to generate consumes data.  RepoUtil examines all of the repo's project.json 
files to determine external dependencies.

We have a lot of differing package dependency versions on libraries in our tree (ie test-runtime, ref assembly packages, 
implementation packages, etc...), for now we will baseline these in the "fixedPackages" section of the RepoData.json file 
but we should try to think about how to merge our "[StableVersions](https://github.com/dotnet/corefx/blob/master/pkg/Microsoft.Private.PackageBaseline/packageIndex.json)" msbuild file with this to remove the redundancy.
