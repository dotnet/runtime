[Consumes API specification](https://github.com/dotnet/buildtools/blob/master/Documentation/RepoCompose.md#consumes-1)

In CoreFx, we'll be using the RepoUtil tool to generate consumes data.  RepoUtil examines all of the repo's project.json 
files to determine external dependencies.

We have a lot of differing package dependency versions on libraries in our tree (ie test-runtime, ref assembly packages, 
implementation packages, etc...).  Our stable versioned dependencies will become fixed package outputs in the consumes API.  Prerelease fixed packages will still need to be listed in the "fixedPackages" section of RepoData.json.
