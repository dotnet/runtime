# Official Releases and Daily Builds of CoreCLR and CoreFX components

If you are not planning on actually making bug fixes or experimenting with new features, then you probably
don't need to build CoreCLR yourself, as the .NET Runtime team routinely does this for you.   

Roughly every three months, the .NET Runtime team publishes a new version of .NET Core to Nuget.   .NET Core's
official home on NuGet is 
 
 * <https://www.nuget.org/packages/Microsoft.NETCore.Runtime.CoreCLR/> 
 
and you can expect to see new versions roughly three months.   However it is also the case that the .NET 
Team publishes **daily builds** of all sorts of packages including those built by the CoreCLR and CoreFX 
repositories.  You can see what is available from

 * <https://dotnet.myget.org/gallery/dotnet-core>, and in particular you can see the builds of 
 * CoreCLR at <https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.Runtime.CoreCLR>
 * NETCore.App at <https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App>

These builds have a version number that follows the the versioning scheme described below (month number/day of month), 
but they also will have a component that indicate which Git Branch the are working from (note these names were
correct as of 1/2018 and may change but the concept of a suffix that designates the branch is likely to persist)

 * preview1 - are daily builds from the 'release/\*' branch where \* is the next official version to be released
 * preview2 - are daily builds from the 'master' branch (where active work happens first (typically))

Thus if your goal is just to get the latest bug fixes and features, you don't need to build CoreCLR yourself you 
can simply add <https://dotnet.myget.org/F/dotnet-core/api/v3/index.json> to your Nuget Feed list and set the
`RuntimeFrameworkVersion` in your project file to a `Microsoft.NETCore.App` version. You need to restore
and publish your application so it includes the runtime (`self-contained`). This is done by setting the
`RuntimeIdentifier` (e.g. `linux-x64`, `win7-x64`).

```
<?xml version="1.0" encoding="utf-8"?>
<configuration>
 <packageSources>
    <add key="dotnet-core" value="https://dotnet.myget.org/F/dotnet-core/api/v3/index.json"/>
 </packageSources>
</configuration>
```
```
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>netcoreapp2.0</TargetFramework>
    <RuntimeFrameworkVersion>2.0.0-preview2-*</RuntimeFrameworkVersion>
    <RuntimeIdentifier>linux-x64</RuntimeIdentifier>
  </PropertyGroup>
</Project>
```
```
$ dotnet restore
$ dotnet publish
$ dotnet bin/Debug/netcoreapp2.0/linux-x64/publish/<app>.dll
```

## Package Version Numbers

Version numbers for Nuget packages look like the following
```
    1.0.24214.01
```
Which have the form
```
    <major>.<minor>.<buildNumberMajor>.<buildNumberMinor>
```

* The major version number represents a compatibility band.   If the next release of the package is not
  backward compatible (most apps that run on version N-1 will run on version N) then this number is increased.
  This number is not likely to change (we care about compatibility alot)  

* The minor number is increased every time interesting new features are added (not just minor bug fixes).
  For CoreCLR we tend to update this every time we create a public release (every 3 months).  

* The Major Build Number is a number that represents a daily build.   The last 2 digits of this build number
  is the **day of the month** of the GIT commit that is being built.   Thus we know in the example above this 
  build's last commit to GIT happened on the 14th day of the month.   The most significant digits represents
  the month count since April 1996.   In the example above 242 represents Jun 2016.   

* The Minor Build number is something that disambiguates different builds that share the same 
  commit (or the different commits on the same day).   It is a sequential number and is typically 1 for
  official builds, and 0 for developer builds.   (You can set the environment variable BuildNumberMinor if
  you wish to set it for your own builds).  

  

See the [Package and File Versioning](https://github.com/dotnet/corefx/blob/master/Documentation/building/versioning.md) page
for more details on how the build version number is generated.   



# Build/Test Status of the repository

As mentioned we build the CoreCLR repository daily, and as part of that build we also run all 
the tests associted with this repository.  Below is a table of the most recent results for all
the different operating systems and architectures that we routinely build.  

If you click on the images below, you can get more details about the build (including the binaries)
and the exact test results (in case your build is failing tests and you are wondering if it is 
something affecting all builds).    

|   | X64 Debug | X64 Release | ARM64 Debug | ARM64 Release |
|---|:-----:|:-------:|:-------:|:-------:|
|**CentOS 7.1**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_centos7.1/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_centos7.1)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_centos7.1/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_centos7.1)|||
|**Debian 8.4**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_debian8.4/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_debian8.4)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_debian8.4/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_debian8.4)|||
|**FreeBSD 10.1**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_freebsd/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_freebsd)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_freebsd/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_freebsd)|||
|**openSUSE 42.1**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_opensuse42.1/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_opensuse42.1)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_opensuse42.1/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_opensuse42.1)|||
|**OS X 10.12**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_osx10.12/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_osx10.12)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_osx10.12/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_osx10.12)|||
|**Red Hat 7.2**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_rhel7.2/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_rhel7.2)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_rhel7.2/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_rhel7.2)|||
|**Ubuntu 14.04**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_ubuntu/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_ubuntu)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_ubuntu/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_ubuntu)|||
|**Ubuntu 16.04**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_ubuntu16.04/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_ubuntu16.04)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_ubuntu16.04/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_ubuntu16.04)|||
|**Ubuntu 16.10**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_ubuntu16.10/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_ubuntu16.10)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_ubuntu16.10/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_ubuntu16.10)|||
|**Windows 8.1**|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_windows_nt/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/debug_windows_nt)|[![x64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/release_windows_nt/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/release_windows_nt)|[![arm64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/arm64_cross_debug_windows_nt/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/arm64_cross_debug_windows_nt)|[![arm64 status](https://ci.dot.net/job/dotnet_coreclr/job/master/job/arm64_cross_release_windows_nt/badge/icon)](http://ci.dot.net/job/dotnet_coreclr/job/master/job/arm64_cross_release_windows_nt)|
