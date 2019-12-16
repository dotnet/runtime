# FreeBSD build instructions

## Disclaimer
Instructions bellow may or may not give you what you want.
Tested on plain FreeBSD 11.3 Azure image

## Prerequisites
This needs to be done once on fresh system.

```sudo pkg install cmake git icu libunwind bash python2 krb5 lttng-ust llvm60 libgit2```

some scripts may still assume /bin/bash exists. To workaround it for now do of needed:
```
sudo ln -s /usr/local/bin/bash /bin/bash
```

This is certainly undesirable and it should be avoided if possible.

# Get bootstrap cli
```
mkdir ~/dotnet
cd ~/dotnet
curl https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-freebsd-x64.tar.gz | tar xfz - 
```
if on 12.x you may also need to set `LD_PRELOAD` to `/usr/lib/libpthread.so` to avoid issue when cli freezes. 


As of summer 2019 this CLI is no longer good enough to build all repos. If that is your case jump to section [Updating CLI](#updating--bootstrap-cli)
Binary snapshot can be obtained from https://github.com/wfurt/blob as dotnet-sdk-freebsd-x64-latest.tgz

## Getting sources
master of source-build pulls in source code of specific snapshot instead of tip of master branches. 
That is generally OK but in case of FreeBSD it may miss some changes crucial for build. 
(or pending un-submitted change) 

```
git clone https://github.com/dotnet/source-build
```

for now, get master for everything (to get https://github.com/dotnet/coreclr/pull/20459 and build coreclr without clang3.9)
```
git submodule init
git submodule update
(cd src/coreclr ; git checkout master)
```

port change from 
```https://github.com/dotnet/corefx/commit/037859ac403ef17879655bb2f2e821d52e6eb4f3```
In ideal case we could sync up to **master** but that brings Arcade changes and **breaks** the build. 

Bootstrap Arcade
```
mkdir -p src/corefx/.dotnet
(cd src/corefx/.dotnet; tar xf /tmp/dotnet.tar)
```
now edit src/corefx/global.json and set tool:dotnet to"3.0.100-preview1-009019"
That should make Arcade happy and it should skip attempt to download and install SDK from Internet.


and apply following patch (to get https://github.com/dotnet/source-build/pull/631)
```diff
[toweinfu@toweinfu-fbsd /usr/home/toweinfu/source-build]$ git diff repos/
diff --git a/repos/core-setup.proj b/repos/core-setup.proj
index 768b6fb..c02f76f 100644
--- a/repos/core-setup.proj
+++ b/repos/core-setup.proj
@@ -4,6 +4,7 @@
   <PropertyGroup>
     <BuildArguments>-ConfigurationGroup=$(Configuration) -PortableBuild=$(PortableBuild) -SkipTests=true </BuildArguments>
     <BuildArguments Condition="$(Platform.Contains('arm'))">$(BuildArguments) -TargetArchitecture=$(Platform) -DisableCrossgen=true -CrossBuild=true</BuildArguments>
+    <BuildArguments Condition="'$(TargetOS)' == 'FreeBSD'">$(BuildArguments) -DisableCrossgen=true</BuildArguments>
     <BuildCommand>$(ProjectDirectory)/build$(ShellExtension) $(BuildArguments) -- /p:BuildDebPackage=false /p:BuildAllPackages=true /p:PreReleaseLabel="preview"</BuildCommand>
     <BuildCommand Condition="$(Platform.Contains('arm'))">$(ArmEnvironmentVariables) $(BuildCommand)</BuildCommand>
     <OfficialBuildId>20180529-01</OfficialBuildId>
diff --git a/repos/coreclr.proj b/repos/coreclr.proj
index 81b8c7b..bb26868 100644
--- a/repos/coreclr.proj
+++ b/repos/coreclr.proj
@@ -5,6 +5,7 @@
     <BuildArguments>$(Platform) $(Configuration) skiptests</BuildArguments>
     <BuildArguments Condition="'$(SkipDisablePgo)' != 'true'">$(BuildArguments) -nopgooptimize</BuildArguments>
     <BuildArguments Condition="'$(OS)' != 'Windows_NT'">$(BuildArguments) msbuildonunsupportedplatform</BuildArguments>
+    <BuildArguments Condition="'$(TargetOS)' == 'FreeBSD'">$(BuildArguments) -clang6.0</BuildArguments>
     <BuildArguments Condition="'$(UseSystemLibraries)' == 'true'">$(BuildArguments) cmakeargs -DCLR_CMAKE_USE_SYSTEM_LIBUNWIND=TRUE</BuildArguments>
     <BuildArguments Condition="$(Platform.Contains('arm'))">$(BuildArguments) skipnuget cross -skiprestore cmakeargs -DFEATURE_GDBJIT=TRUE</BuildArguments>
     <BuildArguments>$(BuildArguments) -PortableBuild=$(PortableBuild)</BuildArguments>
```

Depending of the day and moon phase you may need to get some updates as well. 
If build breaks look for pending PRs with FreeBSD tag or label and pull pending changes. 

## Building

```
# point to bootstrap cli
export DotNetBootstrapCliTarPath=/tmp/dotnet.tar
echo "3.0.100-preview1-009019" > DotnetCLIVersion.txt # version needs to match SDK version from DotNetBootstrapCliTarPath
# get new Roslyn
echo "3.0.0-preview1-03316-04" > BuildToolsVersion.txt
# optional
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1


./build.sh
```

In ideal situation this will build whole sdk. Right now it fails somewhere in cli.
There is problem with rebuild and build will attempt to patch files again and/or make git updates. 

```export SOURCE_BUILD_SKIP_SUBMODULE_CHECK=1```

To build single repo again one can do:
```./build.sh /p:RootRepo=corefx /p:SkipRepoReferences=true ```

## Resolving issues
Rebuild or source-build has issues. 
Often running ```clean.sh``` from top helps. Be careful, that may undo any local pending changes. 

Sometimes it would try to apply patches and it would fail.
You can pass 
```/p:SkipPatches=true``` to top level build.sh script. 


## Running CoreFX tests

Follow steps above to build at least corefx and it's dependencies. 

TBD

## Updating  bootstrap CLI.

As build changes, previous versions of CLI may not be good enough any more. Changes in runtime or build dependency on 3.0 JSON are some example of braking changes. Following steps outline steps to update published CLI to what build needs. It will require other system where builds is supported. As close similarity and availability Linux will be used in examples bellow but Windows or MacOS should also yield same result. 

Often build would ask for slightly different version without actually  have real dependency on it (that is part of rolling updates across repos).
One can cheat in this case and simply:
```
ln -s ~/dotnet/sdk/old_version ~/dotnet/sdk/new_version
```

 

### Finding versions and commit hashes
First we need to find what version are are trying to recreate. That is 'sdk' section in global.json in each repo. As of preview9ih time, this is set to 3.0.100-preview6-012264 and such version will be used in examples. One advantage of using release branches is that it is in coherent state e.g. all repos should need exactly same version. 

Let's get SDK for supported OS. Sync code base to same version you are trying to build on FreeBSD. 
```
./eng/common/build.sh --restore
Downloading 'https://dot.net/v1/dotnet-install.sh'
dotnet-install: Downloading link: https://dotnetcli.azureedge.net/dotnet/Sdk/3.0.100-preview6-012264/dotnet-sdk-3.0.100-preview6-012264-linux-x64.tar.gz
```

When done, there should be build SDK installed in .dotnet directory. (Unless global .net cli is present and already has expected SDK version)

Commit hash for SDK it self is:
```
$ cat .dotnet/sdk/3.0.100-preview6-012264/.version
be3f0c1a03f80492d45396c9f5b855b10a8a0b79
3.0.100-preview6-012264
linux-x64
```
Commit hash for Runtime (core-setup):
```
$ cat .dotnet/shared/Microsoft.NETCore.App/3.0.0-preview6-27804-01/.version
fdf81c6faf7c7e0463d191a3a1d36c25c201e5cb
3.0.0-preview6-27804-01
```
Commit hash for CoreCLR:
```
$ strings .dotnet/shared/Microsoft.NETCore.App/3.0.0-preview6-27804-01/libcoreclr.so | grep @Commit:
@(#)Version 4.700.19.30373 @Commit: 7ec87b0097fdd4400a8632a2eae56612914579ef
```
Commit hash for Core-FX:
```
strings .dotnet/shared/Microsoft.NETCore.App/3.0.0-preview6-27804-01/System.Native.so | grep @Commit:
@(#)Version 4.700.19.30308 @Commit: d47cae744ddfb625db8e391cecb261e4c3d7bb1c
```

### Rebuild SDK (Roslyn, msbuild, etc)
If we need only changes to SDK (like to pick up new Roslyn or MSBuild, life is easy.

get sources:
```
git clone https://github.com/dotnet/core-sdk
cd core-sdk
git checkout be3f0c1a03f80492d45396c9f5b855b10a8a0b79
```

Set variables and assemble SKD without crossgen. (set DropSuffix=true to strip  `preview6` from version). 
```
export DISABLE_CROSSGEN=true
export CLIBUILD_SKIP_TESTS=true
export DropSuffix=false
./build.sh --configuration Release
```
(
Outcome should be in `artifacts/bin/redist/Release/dotnet/sdk/3.0.100-preview6-012264/`

```
rsync -av artifacts/bin/redist/Release/dotnet/sdk/3.0.100-preview6-012264 bsd:~/dotnet/sdk
```

### Rebuilding Runtime
If we also need new runtime, we will need to rebuild CoreCLR and CoreFX as minimum.
Note that the the repose are deeply intertwine and needs to be built together.
Release branch should have compatible repro as well as following hashes should work.

#### Building CoreCLR
```
git clone https://github.com/dotnet/coreclr
cd coreclr
git checkout 7ec87b0097fdd4400a8632a2eae56612914579ef
```

and build 
```
mkdir -p .dotnet
curl https://dotnetcli.blob.core.windows.net/dotnet/Sdk/master/dotnet-sdk-latest-freebsd-x64.tar.gz | tar xfz - -C .dotnet
ln -s 3.0.100-preview-010021 .dotnet/sdk/3.0.100-preview6-012264  # (from global.json)
ln -s 3.0.0-preview-27218-01 .dotnet/shared/Microsoft.NETCore.App/3.0.0-preview6-27804-01
./build.sh -clang6.0 -skiptests -skipmanagedtools -Release /p:SourceRevisionId=7ec87b0097fdd4400a8632a2eae56612914579ef
```

There may be some LibGit2Sharp errors but the build should finish and produce artifacts in `bin/Product/FreeBSD.x64.Release`

If the build fail you can:
- try to sync to head or newer version
- use older compiler (needs cleaning). Usually it is less picky about errors
- LibGit2Sharp - TBD

#### Building CoreFX
This has two parts. We need to build managed bits on supported OS. For example on Linux:

```
git clone https://github.com/dotnet/corefx
cd corefx
git checkout d47cae744ddfb625db8e391cecb261e4c3d7bb1c
./build.sh -c Release /p:osgroup=FreeBSD
```

on FreeBSD we need to build native bits:
```
git clone https://github.com/dotnet/corefx
cd corefx
git checkout d47cae744ddfb625db8e391cecb261e4c3d7bb1c
./src/Native/build-native.sh --clang6.0 -release
```

#### Building core-setup
As this has very little platform dependency it is unlikely this needs to be touched. 
If we want to do this to pick up fix or for consistency than ... TBD

```
src/corehost/build.sh  --configuration Release  --arch x64 --hostver "3.0.0" --apphostver "3.0.0" --fxrver "3.0.0" --policyver "3.0.0" --commithash `git rev-parse HEAD` -portable

```

#### Constructing updated runtime
We will use existing CLI as baseline:
```
cd ~/dotnet/shared/Microsoft.NETCore.App
rsync -av 3.0.0-preview-27218-01/ 3.0.0-preview6-27804-01
```
get CoreFX and CoreCLR updated bits (corefx needs to come first as Wildcard brings Linux ):
```
rsync -av LINUXHOST/corefx/artifacts/bin/runtime/netcoreapp-FreeBSD-Release-x64/*dll 3.0.0-preview6-27804-01
rsync -av corefx/artifacts/bin/native/FreeBSD-x64-Release/*so 3.0.0-preview6-27804-01
rsync -av coreclr/bin/Product/FreeBSD.x64.Release/*  3.0.0-preview6-27804-01
```

if missing add following section to `Microsoft.NETCore.App.deps.json`
```
          "runtimes/freebsd-x64/lib/netcoreapp3.0/System.Runtime.CompilerServices.Unsafe.dll": {
            "assemblyVersion": "4.0.5.0",
            "fileVersion": "4.0.0.0"
          },
```

