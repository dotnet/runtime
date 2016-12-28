# Official Releases and Daily Builds of CoreCLR and CoreFX components

If you are not planning on actually making bug fixes or experimenting with new features, then you probably
don't need to build CoreCLR yourself, as the .NET Runtime team routinely does this for you.   

Roughly every three months, the .NET Runtime team publishes a new version of .NET Core to Nuget.   .NET Core's
official home on NuGet is 
 
 * <https://www.nuget.org/packages/Microsoft.NETCore.Runtime.CoreCLR/> 
 
and you can expect to see new versions roughly three months.   However it is also the case that the .NET 
Team publishes **daily builds** of all sorts of packages including those built by the CoreCLR and CoreFX 
repositories.  You can see what is available from

 * <https://dotnet.myget.org/gallery/dotnet-core>, and in particular you can see the builds of CoreCLR at 
 * <https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.Runtime.CoreCLR>.   
 
Thus if your goal is just to get the latest bug fixes and features, you don't need to build CoreCLR yourself you 
can simply add <https://dotnet.myget.org/F/dotnet-core/api/v3/index.json> to your Nuget Feed list. 

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

|   | Debug | Release |
|---|:-----:|:-------:|
|**CentOS 7.1**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_centos7.1.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_centos7.1)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_centos7.1.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_centos7.1)|
|**Debian 8.4**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_debian8.4.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_debian8.4)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_debian8.4.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_debian8.4)|
|**FreeBSD 10.1**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_freebsd.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_freebsd)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_freebsd.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_freebsd)|
|**openSUSE 13.2**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_opensuse13.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_opensuse13.2)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_opensuse13.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_opensuse13.2)|
|**openSUSE 42.1**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_opensuse42.1.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_opensuse42.1)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_opensuse42.1.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_opensuse42.1)|
|**OS X 10.11**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_osx.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_osx)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_osx.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_osx)|
|**Red Hat 7.2**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_rhel7.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_rhel7.2)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_rhel7.2.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_rhel7.2)|
|**Fedora 23**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_fedora23.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_fedora23)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_fedora23.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_fedora23)|
|**Ubuntu 14.04**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_ubuntu.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_ubuntu)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_ubuntu.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_ubuntu)|
|**Ubuntu 16.04**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_ubuntu16.04.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_ubuntu16.04)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_ubuntu16.04.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_ubuntu16.04)|
|**Ubuntu 16.10**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_ubuntu16.10.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_ubuntu16.10)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_ubuntu16.10.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_ubuntu16.10)|
|**Windows 8.1**|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/debug_windows_nt.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/debug_windows_nt)<br/>[![arm64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/arm64_cross_debug_windows_nt.svg?label=arm64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/arm64_cross_debug_windows_nt)|[![x64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/release_windows_nt.svg?label=x64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/release_windows_nt)<br/>[![arm64 status](https://img.shields.io/jenkins/s/http/dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/arm64_cross_release_windows_nt.svg?label=arm64)](http://dotnet-ci.cloudapp.net/job/dotnet_coreclr/job/master/job/arm64_cross_release_windows_nt)|
