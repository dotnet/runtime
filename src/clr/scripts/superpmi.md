### An overview of using superpmi.py
-------------------------

General information on [SuperPMI](https://github.com/dotnet/coreclr/blob/master/src/ToolBox/superpmi/readme.txt)

------------------------

#### Overview

Although SuperPMI has many uses, setup and use of SuperPMI is not always trivial. superpmi.py is a tool to help automate the use of SuperPMI, augmenting its usefulness. The tool has three different modes, collect, replay, and asmdiffs. Below you will find more specific information on each of the different modes.

**SuperPMI has a large limitation, the replay/asmdiff functionality is sensitive to jitinterface changes. Therefore if there has been an interface change, the new JIT will not load and SuperPMI will fail.**

**At the time of writing collections are done manually on all platforms that support libcoredistools. See before for a full list of supported platforms and where the .mch collection exists.**

#### Supported Platforms

| OS | Arch | Replay | AsmDiffs | mch location |
| --- | --- | --- |--- | --- |
| OSX | x64 |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Windows | x64 |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Windows | x86 |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Windows | arm |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |
| Windows | arm64 |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |
| Ubuntu | x64 |  <ul><li>- [x] </li></ul> |  <ul><li>- [x] </li></ul> |  |
| Ubuntu | arm32 |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |
| Ubuntu | arm64 |  <ul><li>- [ ] </li></ul> |  <ul><li>- [ ] </li></ul> | N/A |

### Default Collections

See the table above for locations of default collections that exist. If there is an mch file that exists, then SuperPMI will automatically download and setup the mch using that location. Please note that, it is possible that the collection is out of date, or there is a jitinterface change which makes the collection invalid. If this is the case, then in order to use the tool a collection will have to be done manually. In order to reproduce the default collections, please see below for what command the default collections are done with.

`/Users/jashoo/coreclr/build.sh x64 checked -skiptests`

`/Users/jashoo/coreclr/build-test.sh x64 checked -priority1`

`/Users/jashoo/coreclr/scripts/superpmi.py collect bash "/Users/jashoo/coreclr/tests/runtest.sh x64 checked" --skip-cleanup`

**Collect**

Given a specific command collect over all of the managed code called by the child process. Note that this allows many different invocations of any managed code. Although it does specifically require that any managed code run by the child process to handle the complus variables set by SuperPMI and defer them to the later. These are below:

```
SuperPMIShimLogPath=<full path to an empty temporary directory>
SuperPMIShimPath=<full path to clrjit.dll, the "standalone" JIT>
COMPlus_AltJit=*
COMPlus_AltJitName=superpmi-shim-collector.dll
```

If these variables are set and a managed exe is run, using for example the dotnetcli, the altjit settings will crash the process.

To avoid this, the easiest way is to unset the variables in the beginning to the root process, and then set them right before calling $CORE_ROOT/corerun.

Note that collection will try to run as much managed code as possible. In order to do this, `COMPlus_ZapDisable=1` is set by default. This can be modified by passing `--use_r2r`.

Also note that collection generates gigabytes of data, most of this data will be removed when the collection is finished and de-dupped into a single mch file. That being said, it is worth mentioning that this process will use 3x the size of the unclean mch file, which to give an example of the size, a collection of the coreclr `priority=1` tests uses roughly `200gb` of disk space. Most of this space will be used in a temp directory, which on Windows will default to `C:\Users\blah\AppData\Temp\...`. It is recommended to set the temp variable to a different location before running collect to avoid running out of disk space. This can be done by simply running `set TEMP=D:\TEMP`.

**Replay**

SuperPMI replay supports faster assertion checking over a collection than running the tests individually. This is useful if the collection includes a larger corpus of data that can reasonably be run against by executing the actual code. Note that this is similar to the PMI tool, with the same limitation, that runtime issues will not be caught by SuperPMI replay only assertions.

**AsmDiffs**

SuperPMI will take two different JITs, a baseline and diff JIT and run the compiler accross all the methods in the mch file. It uses coredistools to do a binary difference of the two different outputs. Note that sometimes the binary will differ, and SuperPMI will be run once again dumping the asm that was output in text format. Then the text will be diffed, if there are differences, you should look for text differences. If there are some then it is worth investigating the asm differences.

It is worth noting as well that SuperPMI gives more stable instructions retired counters for the JIT.