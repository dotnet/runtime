# Building Tests Against Packages

## Usage Scenarios

### Build the product locally and then test what you've built

In this scenario, you produce a product build, including packages, then generate test project.json's which reference the locally built package versions and finally build tests compiling against the local packages.

1. Build the product and packages 
 - ```Build.cmd -BuildTests=false```
2. Generate Test project.json files
 - ```sync.cmd -t -- /p:"SkipCleanPackages=true" /p:"PackagesDrops=[ProjectDir]bin/packages/[Release|Debug]/"```
  - /p:SkipCleanPackages=true is required for release branches where the packages folder is cleaned during every build.
3. Build Tests against packages
 - ```build-tests.cmd -BuildTestsAgainstPackages -- /p:"PackagesDrops=[ProjectDir]bin/packages/[Release|Debug]/"```
  - -BuildTestsAgainstPackages tells the build to use the project.json files you generated in the "Generate Test project.json files" step
  - /p:"PackagesDrops=[ProjectDir]bin/packages/[Release|Debug]/" tells the build to use the packages from your local build drop.

### Download product from an Azure blob

This scenario skips the product build step, and instead downloads packages from Azure blob storage

1. Sync product from Azure
 - ```sync.cmd -ab -AzureAccount=dotnetbuildoutput -AzureToken=******** -Container=[Azure container name] -- /p:"DownloadDirectory=[ProjectDir]Packages\AzureTransfer" /p:"SkipCleanPackages=true"```
2. Generate Test project.json files
 - ```sync.cmd -t -- /p:"SkipCleanPackages=true" /p:"PackagesDrops=[ProjectDir]Packages/AzureTransfer/[Release|Debug]/"```
  - /p:SkipCleanPackages=true is required for release branches where the packages folder is cleaned during every build.
3. Build Tests against packages
 - ```build-tests.cmd -BuildTestsAgainstPackages -- /p:"PackagesDrops=[ProjectDir]Packages/AzureTransfer/[Release|Debug]/"```
  - -BuildTestsAgainstPackages tells the build to use the project.json files you generated in the "Generate Test project.json files" step
  - /p:"PackagesDrops=[ProjectDir]Packages/AzureTransfer/[Release|Debug]/" tells the build to use the packages from the Azure download (DownloadDirectory).

### Use a versions file for specifying package versions

This scenario uses a versions file (https://github.com/dotnet/versions/blob/master/build-info/dotnet/corefx/master/Latest_Packages.txt, for example) to determine what package versions to build tests against.

1. Generate Test project.json files using a 'versions' file.
 -   ```sync.cmd -t -- /p:"SkipCleanPackages=true" /p:"VersionsFiles=[local version file path]"```
  - /p:SkipCleanPackages=true is required for release branches where the packages folder is cleaned during every build.
2. Build Tests against packages
 - ```build-tests.cmd -BuildTestsAgainstPackages -- /p:"PackagesDrops=[ProjectDir]bin/packages/[Release|Debug]/"```
  - -BuildTestsAgainstPackages tells the build to use the project.json files you generated in the "Generate Test project.json files" step
  - /p:"PackagesDrops=[ProjectDir]bin/packages/[Release|Debug]/" tells the build to use the packages from your local build drop.
   - If the package versions you are referencing have been published publically, you can omit the "PackagesDrops" property.

## Common Questions

- **How do I know it worked?**  The best way is to look in the log for the compilation line ("csc.exe") and ensure that its references now point to packages (packages\blah) where previously they pointed to build product binaries (bin\blah).

- **Why are there build failures?**  Not all of our tests build nicely against package references due to differences in the public surface area (compiling against the reference assembly versus an implementation assembly).  In cases where we were unable to sync / restore (packages were unavailable or other restore problems), we've opted those projects out of this process by adding "KeepAllProjectReferences" or "KeepProjectReference" (added to a Project Reference's metadata) to the test project.

- **Where are the generated project.json files?** Generated project.json files get created under "[ProjectDir]bin/obj/generated".  
