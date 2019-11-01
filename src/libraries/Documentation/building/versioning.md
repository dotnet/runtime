Package and File Versioning
===========================

We have now added package versioning and assembly file versioning when building the repo. This versioning works in both Windows and non-Windows platforms. Also, this versioning applies for both the managed and native libraries we produce. We had a few constraints to take into account, for example:
- Version needed to be higher than the versions that we have already shipped.
- There needs to be an ability to have multiple versions per day.
- Versions need to be always increasing.
- Version needs to be lower than 65535(unsigned short int max) since the version is used as assembly file version which has that constraint.
- Version needs to be reproducible.
- (Nice to have) We shouldn't have the need to check in a file containing the version or revision number.

Taking into account all these constraints we came up with the new versioning. The version is calculated per build by [a targets file that lives in buildtools](https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.Build.Tasks/PackageFiles/versioning.targets), and depending on what we are building(native/managed, Windows/non-Windows) it will generate versioning assets that will be used during the build to be able to produce build output with the right versions.

How is the version calculated
=============================

The version is composed by two parts; the build number major and the build number minor. An example looks something like 00001.01, where the first five digits represent the build number major, and the two last digits after the dot represent the build number minor.

Calculating BuildNumberMajor
----------------------------

The BuildNumberMajor is represented by 5 digits, and is determined by calculating the time that has happened between the latest commit's date(SeedDate) and a VersionComparisonDate that gets passed in. In the case where VersionComparisonDate is not passed in, we will use April 1st 1996 as default. The reason why we use this specific date, is to ensure that the version produced by SeedDate is higher than the already shipped versions of the same library. The first portion of the BuildNumberMajor (first 3 digits) represent the number of month(s) since the VersionComparisonDate. The second portion of the BuildNumberMajor (last 2 digits) represent the day of the month of SeedDate. This part of the version is reproducible.

Calculating BuildNumberMinor
----------------------------

The BuildNumberMinor represent the build # of the day this version is. This number increases by 1, and it starts in 01. Given that there is no way for a dev machine to know how many builds have happened that day, this number is calculated by our VSO official builds and needs to be passed in. Since dev builds usually don't have a BuildNumberMinor (unless it's passed in manually), they will always have a BuildNumberMinor=0. For example, a Official build version would look something like `00001.01` while a dev build version will look like `00001.0`

Packages Version
----------------

In the case of packages, there is one small difference compared to the assembly file version, which is the separator of BuildNumberMajor and BuildNumberMinor. As noted above, for assembly file version we use a dot `.` to separate these two numbers, but for packages we use a dash `-`. This is due to the different requirements that package names and file versions have. For example, the package version of file 'mylibrary.dll - version 00001.01' would be `mylibrary.1.0.0-prerelease-00001-01.nupkg`

How does the Official Build workflow works
------------------------------------------

Our Official Builds are a little different than the regular dev flow, and that is because Official Builds need the ability to not only force a specific BuildNumberMinor, but also a BuildNumberMajor. The way they do it, is by passing in the parameter `OfficialBuildId` which specifies the SeedDate that should be used and the revision of the build. For example, the following invocation: `build.cmd -OfficialBuildId=20160523.99` will use May 23 2016 as the SeedDate to generate the version, and it will set '99' as the BuildNumberMinor. With this functionality, our OfficialBuilds are able to have an orchestrator that triggers different builds and force all of them to have the same version.

Getting the version of a native binary in non-Windows platforms
========================================================

When trying to get a version of a native binary in non-Windows, there are two ways to do it:
- Using the Linux tool `sccs what(1)` by installing the tool (if it doesn't come by default with the OS) and then running something like `what myNativeNonWindowsBinary.so`. For more information on the tool, [click here](https://www.ibm.com/support/knowledgecenter/ssw_aix_72/com.ibm.aix.cmds6/what.htm)
- The other way to do it if you don't want to install additional tools, is by using `strings+grep`, and the way to do it is by running `strings myNativeNonWindowsBinary.so | grep "@(#)"`

How to force a dev build to produce a specific version
======================================================

If you need to manually specify the version you want to produce your build output with, you can accomplish this by running the following from the root of the repo:
- `build.cmd /p:BuildNumberMajor=00001 /p:BuildNumberMinor=01` in Windows
- `build.sh  /p:BuildNumberMajor=00001 /p:BuildNumberMinor=01` in non-Windows

Where is the version being consumed
===================================

The version we produce by our calculations is mainly used in two places:
- As the [Assembly File Version](https://msdn.microsoft.com/en-us/library/51ket42z(v=vs.110).aspx)
- As the packages version number

To get more information on where are we doing the calculations for the versioning, you can [click here](https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.Build.Tasks/PackageFiles/versioning.targets) to find the targets file where we create the versioning assets, and [here](https://github.com/dotnet/buildtools/blob/master/src/Microsoft.DotNet.Build.Tasks/GenerateCurrentVersion.cs) to see the code on where we calculate BuildNumberMajor and BuildNumberMinor.