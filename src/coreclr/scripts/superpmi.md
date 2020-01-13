# An overview of using superpmi.py

General information on SuperPMI can be found [here](../src/ToolBox/superpmi/readme.md)

## Overview

Although SuperPMI has many uses, setup and use of SuperPMI is not always trivial.
superpmi.py is a tool to help automate the use of SuperPMI, augmenting its usefulness.
The tool has three primary modes: collect, replay, and asmdiffs.
Below you will find more specific information on each of the different modes.

## General usage

From the usage message:

```
usage: superpmi.py [-h] {collect,replay,asmdiffs,upload,list-collections} ...

Script to run SuperPMI replay, ASM diffs, and collections. The script also
manages the Azure store of precreated SuperPMI collection files. Help for each
individual command can be shown by asking for help on the individual command,
for example `superpmi.py collect --help`.

positional arguments:
  {collect,replay,asmdiffs,upload,list-collections}
                        Command to invoke

optional arguments:
  -h, --help            show this help message and exit
```

The simplest usage is to replay using:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py replay
```

In this case, everything needed is found using defaults:

- The processor architecture is assumed to be the current default (e.g., x64).
- The build type is assumed to be Checked.
- Core_Root is found by assuming superpmi.py is in the normal location in the
clone of the repo, and using the processor architecture, build type, and current
OS, to find it.
- The SuperPMI tool and JIT to use for replay is found in Core_Root.
- The collection to use for replay is the default that is found in the
precomputed collections that are stored in Azure.

If you want to use a specific MCH file collection, use:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py replay -mch_file f:\spmi\collections\tests.pmi.Windows_NT.x64.Release.mch
```

To generate ASM diffs, use the `asmdiffs` command. In this case, you must specify
the path to a baseline JIT compiler, e.g.:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py asmdiffs f:\jits\baseline_clrjit.dll
```

ASM diffs requires the coredistools library. The script attempts to either find
or download an appropriate version that can be used.

## Collections

SuperPMI requires a collection to enable replay. You can do a collection
yourself, but it is more convenient to use existing precomputed collections.
Superpmi.py can automatically download existing collections

Note that SuperPMI collections are sensitive to JIT/EE interface changes. If
there has been an interface change, the new JIT will not load and SuperPMI
will fail.

**At the time of writing, collections are done manually. See below for a
full list of supported platforms and where the .mch collection exists.**

## Supported Platforms

| OS      | Arch  | Replay                    | AsmDiffs                  | MCH location |
| ---     | ---   | ---                       | ---                       | --- |
| OSX     | x64   |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Windows | x64   |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Windows | x86   |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Windows | arm   |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |
| Windows | arm64 |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |
| Ubuntu  | x64   |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Ubuntu  | arm32 |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |
| Ubuntu  | arm64 |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |

## Default Collections

See the table above for locations of default collections that exist. If there
is an MCH file that exists, then SuperPMI will automatically download and
use the MCH from that location. Please note that it is possible that the
collection is out of date, or there is a jitinterface change which makes the
collection invalid. If this is the case, then in order to use the tool a
collection will have to be done manually. In order to reproduce the default
collections, please see below for what command the default collections are
done with.

## Collect

Example commands to create a collection:

```
/Users/jashoo/runtime/src/coreclr/build.sh x64 checked
/Users/jashoo/runtime/src/coreclr/build-test.sh x64 checked -priority1
/Users/jashoo/runtime/src/coreclr/scripts/superpmi.py collect bash "/Users/jashoo/runtime/src/coreclr/tests/runtest.sh x64 checked"
```

Given a specific command, collect over all of the managed code called by the
child process. Note that this allows many different invocations of any
managed code. Although it does specifically require that any managed code run
by the child process to handle the COMPlus variables set by SuperPMI and
defer them to the latter. These are below:

```
SuperPMIShimLogPath=<full path to an empty temporary directory>
SuperPMIShimPath=<full path to clrjit.dll, the "standalone" JIT>
COMPlus_AltJit=*
COMPlus_AltJitName=superpmi-shim-collector.dll
```

If these variables are set and a managed exe is run, using for example the
dotnet CLI, the altjit settings will crash the process.

To avoid this, the easiest way is to unset the variables in the beginning to
the root process, and then set them right before calling `$CORE_ROOT/corerun`.

You can also collect using PMI instead of running code. Do with with the `--pmi` and `-pmi_assemblies`
arguments. E.g.:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py collect --pmi -pmi_assemblies f:\assembly_store -output_mch_path f:\collections\my_collection.mch
```

Note that collection generates gigabytes of data. Most of this data will
be removed when the collection is finished. That being said, it is worth
mentioning that this process will use 3x the size of the unclean MCH file,
which to give an example of the size, a collection of the coreclr
`priority=1` tests uses roughly `200gb` of disk space. Most of this space
will be used in a temp directory, which on Windows will default to
`C:\Users\blah\AppData\Temp\...`. It is recommended to set the temp variable
to a different location before running collect to avoid running out of disk
space. This can be done by simply running `set TEMP=D:\TEMP`.

## Replay

SuperPMI replay supports faster assertion checking over a collection than
running the tests individually. This is useful if the collection includes a
larger corpus of data that can reasonably be run against by executing the
actual code, or if it is difficult to invoke the JIT across all the code in
the collection. Note that this is similar to the PMI tool, with the same
limitation, that runtime issues will not be caught by SuperPMI replay only
assertions.

## Asm Diffs

SuperPMI will take two different JITs, a baseline and diff JIT and run the
compiler accross all the methods in the MCH file. It uses coredistools to do
a binary difference of the two different outputs. Note that sometimes the
binary will differ, and SuperPMI will be run once again dumping the asm that
was output in text format. Then the text will be diffed, if there are
differences, you should look for text differences. If there are some then it
is worth investigating the asm differences.

superpmi.py can also be asked to generate JitDump differences in addition
to the ASM diff differences generated by default.

It is worth noting as well that SuperPMI gives more stable instructions
retired counters for the JIT.
