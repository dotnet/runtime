# How to correlate commits with builds

Recently I checked in a change to one of our various dotnet repos (coreclr) and 
I wanted to figure out when the change would appear in our various NuGet 
packages and installers. I didn't know how the changes flowed through our 
build, setup, and publication processes so I asked around (thanks @dagood!) and 
wrote up what I learned. If you discover this doc is inaccurate or incomplete, 
kindly update it! 

## How Release works from 10,000 ft

1. Every night an automated build runs on the source of each dotnet repo and
produces some binaries that are placed in NuGet packages on MyGet. For example
[this CoreCLR package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR)
is one of the packages produced by the 
[CoreCLR repo](https://github.com/dotnet/coreclr).
These packages are generally not intended for distribution to customers, they 
are only containers to exchange binaries.
2. Every night a seperate automated build for the 
[core-setup](https://github.com/dotnet/core-setup) repo produces 
[installers](https://github.com/dotnet/core/blob/master/daily-builds.md)
and the [Microsoft.NetCore.App NuGet package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App).
These packages aggregate the binaries that were built from many other repos 
and this is what customers use - though more often Preview and RTM builds 
instead of the less tested daily builds. 
The daily builds of installers and Microsoft.NetCore.App do not necessarily
have the latest builds from each constituent repo, thus the build versions
from individual repos may not match the build version of Microsoft.NetCore.App 
or an installer that aggregated them.

## Our starting point

In my case I had a git commit: [435a69a9aca037f348a09e00dffef8a0888682a7 
in the coreclr repo](https://github.com/dotnet/coreclr/commit/435a69a9aca037f348a09e00dffef8a0888682a7) 
and I wanted to know where this code had propagated to. Alternatively
you may be starting from a particular package or installer and wondering
what source it contains. Lets start exploring!


## Where are the daily build NuGet packages published?

For many of the dotnet repos such as CoreCLR the daily builds are posted to the 
[dotnet-core](https://dotnet.myget.org/gallery/dotnet-core) feed. Other repos or 
specific branches might publish elsewhere in which case hopefully you can find 
that in the repo docs or by searching [the gallery](https://dotnet.myget.org/gallery) 
for an appropriate sounding feed name.


## What (intermediate) NuGet packages does a given repo produce?

Whenever a dotnet repo publishes new builds it makes a commit in the 
[dotnet/versions repo](https://github.com/dotnet/versions) indicating which 
packages got published. For example during the night after I merged my 
example CoreCLR change 
[this commit](https://github.com/dotnet/versions/commit/cf8930fbe52e5eacf8ab0d7fb06f032d19cda5d5#diff-5f6099c37f777c410c4397b3f1e38870)
was merged in dotnet/versions. Looking at 
[Last_Build_Packages.txt](https://github.com/dotnet/versions/blob/master/build-info/dotnet/coreclr/master/Last_Build_Packages.txt) 
which was modified in that commit you can see a list of NuGet packages that 
CoreCLR repo publishes.


## What git commit was a NuGet package built from (answer via MyGet)?

Pick an arbitrary NuGet package produced by your repo of interest and look 
for it on the MyGet [gallery](https://dotnet.myget.org/gallery). For example
one of the [coreclr packages](https://dotnet.myget.org/feed/dotnet-core/package/nuget/runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR) 
produced by the [coreclr repo](https://github.com/dotnet/coreclr). 

Right in the description text there is an embedded git commit hash and lower 
on the page is the Package History section with links to every build of the 
package that has been published. For example the 
[2.2.0-preview1-26608-04](https://dotnet.myget.org/feed/dotnet-core/package/nuget/runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR/2.2.0-preview1-26608-04) 
build description says:

> Internal implementation package not meant for direct consumption. Please do not reference directly. The .NET Core runtime,
called CoreCLR, and the base library, called System.Private.CoreLib. It includes the garbage collector, JIT compiler, base .NET
data types and many low-level classes. 311322beb96c5475fd7030fcd2f6e7ff14918853 When using NuGet 3.x this package
requires at least version 3.4.

This NuGet package has all the code from commit [311322beb96c5475fd7030fcd2f6e7ff14918853](https://github.com/dotnet/coreclr/commit/311322beb96c5475fd7030fcd2f6e7ff14918853) 


## What git commit was a NuGet package built from (answer via package inspection)?

You can explore the contents of a NuGet package by changing the extension from 
.nupkg to .zip, then extract it with any tool that handles .zip. In the root 
of the extracted folder there is a version.txt file which shows the git 
commit hash.

In the [Microsoft.NetCore.App NuGet package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App)
the file Microsoft.NETCore.App.versions.txt contains the git commit hashes for 
the corefx and coreclr builds that were aggregated as well.


## What git commit was a particular binary built from?

The git commit hash is appended to the version resource in many of our 
binaries. You can use any tool that views file versions such as a debugger 
or filever.


## What repo build version has a particular commit I am looking for?

There is no direct map from commits to build versions, instead you need to find 
an arbitrary NuGet package produced by the repo the commit is in (see above) 
and then search different package build versions to find the one that contains 
the commit. You can often make a good initial guess by noting the time the 
commit was merged and pick the next build after that. 

For example I wanted to find the first build from the CoreCLR repo that 
contained commit 
[435a69a9aca037f348a09e00dffef8a0888682a7](https://github.com/dotnet/coreclr/commit/435a69a9aca037f348a09e00dffef8a0888682a7).
I arbitrarily chose 
[this package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR) 
that is created by the CoreCLR repo. I checked the build that was uploaded the 
evening after the commit 
[2.2.0-preview1-26608-04](https://dotnet.myget.org/feed/dotnet-core/package/nuget/runtime.win-x64.Microsoft.NETCore.Runtime.CoreCLR/2.2.0-preview1-26608-04).
That package was built from commit
[311322beb96c5475fd7030fcd2f6e7ff14918853](https://github.com/dotnet/coreclr/commit/311322beb96c5475fd7030fcd2f6e7ff14918853)
(determined as above). The commit was more recent than mine so I know my 
change was included in CoreCLR build 2.2.0-preview1-26608-04.


## What build of Microsoft.NetCore.App NuGet package has a particular constituent repo commit I am looking for (answer via MyGet)?

1. Determine the first build from the repo that has the commit (see above)
For example above I determined that commit 
[435a69a9aca037f348a09e00dffef8a0888682a7](https://github.com/dotnet/coreclr/commit/435a69a9aca037f348a09e00dffef8a0888682a7)
in the coreclr repo first appears in the CoreCLR build version 
2.2.0-preview1-26608-04.

2. In the [commit history of the core-setup repo](https://github.com/dotnet/core-setup/commits/master) 
look for commits that say 'Update your_repo_name to some_version' 
where some_version is at least as recent as the one that has your commit. 
[This one](https://github.com/dotnet/core-setup/commit/8a48d863ad01ccd0763b7f3fab487503f5b75625) 
updated CoreCLR to preview1-26609-02. Then you search the MyGet feed for the 
[Microsoft.NetCore.App package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App) 
looking for one with an embedded git hash in the description text that is at 
least as recent. In this case 
[this package](https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App/2.2.0-preview1-26610-01) 
was exactly at that commit.


## What build of Microsoft.NetCore.App NuGet package has a particular constituent repo commit I am looking for (answer via inspecting a package)?

If you have a particular Microsoft.NETCore.App NuGet package already downloaded 
you can look at Microsoft.NETCore.App.versions.txt in the root of the package 
to see the git hashes of various repos that were used to compose the build.


## What build of the installers has a particular constituent repo commit I am looking for?

1. Determine the first build of Microsoft.NetCore.App which has the commit 
(see above). For example I determined 
[Microsoft.NetCore.App 2.2.0-preview1-26610-01](https://dotnet.myget.org/feed/dotnet-core/package/nuget/Microsoft.NETCore.App/2.2.0-preview1-26610-01)
contains coreclr commit 
[435a69a9aca037f348a09e00dffef8a0888682a7](https://github.com/dotnet/coreclr/commit/435a69a9aca037f348a09e00dffef8a0888682a7)

2. Daily builds for both the runtime and the full SDK can be found 
[here](https://github.com/dotnet/core/blob/master/daily-builds.md). For the
runtime build, pick the same version as the Microsoft.NetCore.App NuGet package 
you just determined in step (1). For the SDK if you download one of these in 
zip form you can browse to .\shared\Microsoft.NETCore.App and then build number 
of the M.N.A package is used as the subdirectory name. Make sure the version
is recent enough to contain the changes you are looking for.