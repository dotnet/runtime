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

## Windows dump on Windows

### Debug with WinDbg

1. Install [dotnet-sos global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos).
2. Run `dotnet sos install` (This has an architecture flag to install diferent plugin versions for specific arch scenarios).
3. Load the dump with a recent WinDbg version for it to load sos automatically (if not you can run `.update sos`). It is important that bitness of WinDbg matches the bitness of the dump.
4. Then run the following commands:

```script
!setclrpath <path to core root or testhost where coreclr is>
.sympath+ <directory with symbols (for library tests these are in testhost dir)>
```
### Analyze with dotnet-dump

1. Install [dotnet-dump global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).
2. Run: `dotnet-dump analyze <path-to-dump>`
3. Then run the following commands:

```script
setclrpath (To verify an incorrect DAC hasn't been loaded).
setclrpath <path to core root or testhost where coreclr is>
setsymbolserver -directory <directory with symbols (for library tests these are in testhost dir)>
```

## Linux dumps on Windows

In order to debug a Linux dump on Windows, you will have to first go to the PR/CI build
that sent the test run and download the cross DAC.

Download the [`CoreCLRCrossDacArtifacts`](https://dev.azure.com/dnceng/public/_apis/build/builds/%BUILDID%/artifacts?artifactName=CoreCLRCrossDacArtifacts&api-version=6.0&%24format=zip), then extract it, and copy the matching flavor of the DAC with your dump and extract it in the same location where coreclr binary is.

### Debug with WinDbg

1. Install [dotnet-sos global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos).
2. Run `dotnet sos install` (This has an architecture flag to install diferent plugin versions for specific arch scenarios).
3. Load the dump with a recent WinDbg version for it to load sos automatically (if not you can run `.update sos`). It is important that bitness of WinDbg matches the bitness of the dump.
4. Then run the following commands:

```script
!setclrpath <path to core root or testhost where coreclr is>
.sympath+ <directory with symbols (for library tests these are in testhost dir)>
```
### Analyze with dotnet-dump

1. Install [dotnet-dump global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-dump).
2. Run: `dotnet-dump analyze <path-to-dump>`
3. Then run the following commands:

```script
setclrpath (To verify an incorrect DAC hasn't been loaded).
setclrpath <path to core root or testhost where coreclr is>
setsymbolserver -directory <directory with symbols (for library tests these are in testhost dir)>
```

## Linux dumps on Linux

### Debug with LLDB

1. Install [dotnet-sos global tool](https://docs.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-sos).
2. Run `dotnet sos install` (This has an architecture flag to install diferent plugin versions for specific arch scenarios).
3. Load the dump by running `lldb -c <path-to-dmp> <host binary used (found in testhost)>`
4. Run the following commands:

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
setclrpath (To verify an incorrect DAC hasn't been loaded).
setclrpath <path to core root or testhost where coreclr is>
setsymbolserver -directory <directory with symbols (for library tests these are in testhost dir)>
```

## MacOS dumps

Instructions for debugging dumps on MacOS the same as [Linux](#linux-dumps-on-linux); however there are a couple of caveats.

1. It's only supported to debug them in `dotnet-dump` if it's a runtime generated dump. This includes hang dumps and dumps generated by `createdump`, `dotnet-dump` and the runtime itself. 
2. If it's a system dump, then only `lldb` works.
