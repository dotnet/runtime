OVERVIEW
========

This directory contains the SuperPMI tool used for testing the .NET
just-in-time (JIT) compiler.

SuperPMI has two uses:
1. Verification that a JIT code change doesn't cause any asserts.
2. Finding test code where two JIT compilers generate different code, or
verifying that the two compilers generate the same code.

Case #1 is useful for doing quick regression checking when making a source
code change to the JIT compiler. The process is: (a) make a JIT source code
change, (b) run that newly built JIT through a SuperPMI run to verify no
asserts have been introduced.

Case #2 is useful for generating assembly language diffs, to help analyze the
impact of a JIT code change.

SuperPMI works in two phases: collection and playback. In the collection
phase, the system is configured to collect SuperPMI data. Then, run any
set of .NET managed programs. When these managed programs invoke the JIT
compiler, SuperPMI gathers and captures all information passed between the
JIT and its .NET host. In the playback phase, SuperPMI loads the JIT directly,
and causes it to compile all the functions that it previously compiled,
but using the collected data to provide answers to various questions that
the JIT needs to ask. The .NET execution engine (EE) is not invoked at all.


TOOLS
==========

There are two native executable tools: superpmi and mcs. There is a .NET Core
C# program that is built as part of the coreclr repo tests build called
superpmicollect.exe.

All will show a help screen if passed -?.


COLLECTION
==========

Set the following environment variables:

    SuperPMIShimLogPath=<full path to an empty temporary directory>
    SuperPMIShimPath=<full path to clrjit.dll, the "standalone" JIT>
    COMPlus_AltJit=*
    COMPlus_AltJitName=superpmi-shim-collector.dll

(On Linux, use libclrjit.so and libsuperpmi-shim-collector.so. On Mac,
use libclrjit.dylib and libsuperpmi-shim-collector.dylib.)

If collecting using crossgen, set COMPlus_AltJitNgen=* instead of, or in
addition to, COMPlus_AltJit=*.

Then, run some managed programs. When done running programs, un-set these
variables.

Now, you will have a large number of .mc files. Merge these using the mcs
tool:

    mcs -merge base.mch *.mc

One benefit of SuperPMI is the ability to remove duplicated compilations, so
on replay only unique functions are compiled. Use the following to create a
"unique" set of functions:

    mcs -removeDup -thin base.mch unique.mch

Note that -thin is not required. However, it will delete all the compilation
result collected during the collection phase, which makes the resulting MCH
file smaller. Those compilation results are not required for playback.

Use the superpmicollect.exe tool to automate and simplify this process.


PLAYBACK
========

Once you have a merged, de-duplicated MCH collection, you can play it back
using:

    superpmi unique.mch clrjit.dll

You can do this much faster by utilizing all the processors on your machine,
and replaying in parallel, using:

    superpmi -p unique.mch clrjit.dll

