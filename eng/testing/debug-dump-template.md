# Debugging a CI dump

This document describes how to debug a CI/PR test dump by downloading assets from helix, using a dotnet tool called `runfo`.

## What is runfo?

Runfo is a dotnet global tool that helps get information about helix test runs and azure devops builds. For more information see [this](https://github.com/jaredpar/runfo/tree/master/runfo#runfo)

### How do I install it?

You just need to run:

```script
dotnet tool install --global runfo
```

If you already have it installed, make sure you have at least version `0.6.1` installed, which contains support to download helix payloads. If you don't have the latest version just run:

```script
dotnet tool update --global runfo
```

## Download helix payload containing symbols:

You can just achieve this by running:

```script
runfo get-helix-payload -j %JOBID% -w %WORKITEM% -o <out-dir>
```

> NOTE: if the helix job is an internal job, you need to pass down a [helix authentication token](https://helix.dot.net/Account/Tokens) using the `--helix-token` argument.

This will download the workitem contents under `<out-dir>\workitems\` and the correlation payload under: `<out-dir>\correlation-payload\`. 

> The correlation payload is usually the testhost or core root, which contain the runtime and dotnet host that we use to run tests.

Once you have those assets, you will need to extract the testhost or core root. Then extract the workitem assets into the same location where coreclr binary is.

## Windows dump on windows

### Debug with WinDbg

1. Install [dotnet-sos global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos).
2. Load the dump with a recent WinDbg version for it to load sos automatically (if not you can run `.update sos`).
3. Then run the following commands:

```script
!setclrpath <path to core root or testhost where coreclr is>
.sympath+ <directory with symbols (for library tests these are in testhost dir)>
```
### Analyze with dotnet-dump

1. Install [dotnet-dump global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).
2. Run: `dotnet-dump analyze <path-to-dump>`
3. Then run the following commands:

```script
setclrpath <you should see `Load path for DAC/DBI: '<none>'`>
setclrpath <path to core root or testhost where coreclr is>
setsymbolserver -directory <directory with symbols (for library tests these are in testhost dir)>
```

## Linux dumps on windows

In order to debug a Linux dump on Windows, you will have to first go to the PR/CI build
that sent the test run and download the cross DAC.

Go to your build by navigating to [your build](https://dnceng.visualstudio.com/public/_build/results?buildId=%BUILDID%&view=artifacts&type=publishedArtifacts) and download the `CoreCLRCrossDacArtifacts`, then choose the cross DAC you need depending on the flavor.

Once you've downloaded that artifact, make sure to extract it in the same location where coreclr binary is.

### Debug with WinDbg

1. Install [dotnet-sos global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos).
2. Load the dump with a recent WinDbg version for it to load sos automatically (if not you can run `.update sos`).
3. Then run the following commands:

```script
!setclrpath <path to core root or testhost where coreclr is>
.sympath+ <directory with symbols (for library tests these are in testhost dir)>
```
### Analyze with dotnet-dump

1. Install [dotnet-dump global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).
2. Run: `dotnet-dump analyze <path-to-dump>`
3. Then run the following commands:

```script
setclrpath <you should see `Load path for DAC/DBI: '<none>'`>
setclrpath <path to core root or testhost where coreclr is>
setsymbolserver -directory <directory with symbols (for library tests these are in testhost dir)>
```

## Linux dumps on Linux

### Debug with LLDB

1. Install [dotnet-sos global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos).
2. Load the dump by running `lldb -c <path-to-dmp> <host binary used (found in testhost)>`
3. Run the following commands:

```script
setclrpath <path to core root or testhost where coreclr is>
sethostruntime '<path to your local dotnet runtime or testhost where coreclr is>'
setsymbolserver -directory <directory with symbols (for library tests these are in testhost dir)>
loadsymbols (if you want to resolve native symbols)
```

### Analyze with dotnet-dump

1. Install [dotnet-dump global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).
2. Run: `dotnet-dump analyze <path-to-dump>`
3. Then run the following commands:

```script
setclrpath <you should see `Load path for DAC/DBI: '<none>'`>
setclrpath <path to core root or testhost where coreclr is>
setsymbolserver -directory <directory with symbols (for library tests these are in testhost dir)>
```

## MacOS dumps

MacOS instructions are the same as [Linux](#linux-dumps-on-linux); however MacOS has some caveats.

1. It's only supported to debug them in `dotnet-dump` if it's a `createdump` dump.
2. If it's a system dump, then only `lldb` works.
