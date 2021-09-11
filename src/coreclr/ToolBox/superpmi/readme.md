# Overview

This directory (`src/coreclr/ToolBox/superpmi` in the GitHub
https://github.com/dotnet/runtime repository) contains the SuperPMI
tool used for testing the .NET just-in-time (JIT) compiler.

## Purpose

SuperPMI has two primary uses:
1. Verification that a JIT code change doesn't cause any asserts.
2. Finding test code where two JIT compilers generate different code, or
verifying that the two compilers generate the same code.

Case #1 is useful for doing quick regression checking when making a source
code change to the JIT compiler. The process is: (a) make a JIT source code
change, (b) run that newly built JIT through a SuperPMI run to verify no
asserts have been introduced.

Case #2 is useful for generating assembly language diffs, to help analyze the
impact of a JIT code change.

## SuperPMI architecture

SuperPMI works in two phases: collection and playback.

In the collection phase, the system is configured to collect SuperPMI data.
Then, run any set of .NET managed programs. When these managed programs invoke the JIT
compiler, SuperPMI gathers and captures all information passed between the
JIT and its .NET host. This data is post-processed to remove essentially
duplicate function information, and is collected into one or just a few
files.

In the playback phase, SuperPMI loads the JIT directly, and causes it to
compile all the functions that were previously compiled in the collection
phase, but using the collected data to provide answers to various questions
that the JIT needs to ask. The .NET execution engine (EE) is not invoked at all.
When doing playback for assertion checking, only a single JIT is loaded and
used for compilation. When doing playback to check for assembly diffs, both
a "baseline" and a "diff" compiler are loaded. Each JIT is asked to compile
each recorded function. The generated results are compared with a built-in
SuperPMI "near differ", which depends on an external disassembler component
called `coredistools`. Typically, scripting has SuperPMI generate a list of
functions with differences, then the script re-invokes each JIT to generate
disassembly output (using `COMPlus_JitDisasm`) for each differing function.
These are then compared either visually, or with the jitutils tool
`jit-analyze`, or both.


# Tools

There are two native executable tools: `superpmi` and `mcs`. To do the collection,
there is the `superpmi-shim-collector` binary (`.dll` or `.so` or `.dylib`).

To harness collection, there is a .NET Core C# program that is built as
part of the coreclr tests build called superpmicollect.exe
(source: src/tests/JIT/superpmi in https://github.com/dotnet/runtime repository).
This tool also functions as a SuperPMI collection and playback unit test.

The superpmicollect tool is also being moved to the jitutils repository
(https://github.com/dotnet/jitutils).

Each tool will show a help screen if passed `-?`.

Finally, there is a Python script that harnesses SuperPMI collection,
playback, and other functions, such as download of existing SuperPMI
collections from well-known locations. This tool is `superpmi.py`,
found in `src/corclr/scripts` in the dotnet/runtime repository. See
[superpmi.md](../../../scripts/superpmi.md) for more details.


# Collection

To manually do a collection (not using the `superpmi.py` script or
`superpmicollect.exe` tool), follow the following steps.

## Overall collection process

First, build the `dotnet/runtime` repo, which builds the `superpmi`, `mcs`,
and `superpmi-shim-collector` programs, along with the rest of coreclr,
and places them in the same native code directory as the JIT and the rest
of coreclr, e.g., `f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\superpmi.exe`
for a `dotnet/runtime` repo rooted at the `f:\gh\runtime` directory, and
built on Windows for the x64 Checked architecture / build flavor combination.

The SuperPMI collection process requires a lot of disk space. How much will
depend on the number, size, and complexity of the functions the JIT needs to
compile. Make sure that the disk where the collected data will be collected
is sufficiently large. It is also best if the data is written on an SSD, to
speed up the disk operations.

These are the general steps that are followed when doing a SuperPMI collection:

1. Collect .MC files. Set up for collection, then cause the JIT to be invoked
by either running a scenario, running tests, crossgen2 compiling assemblies,
or using PMI to force the JIT to compile functions in an assembly.
During collection, the data for each JIT compiled function is stored
in a uniquely named file with a ".MC" filename extension (for "method context").
2. Merge .MC files to .MCH file. We want all the generated data to be merged to a
single file (or some smaller set of files), to both be more manageable, and
to collect the data for multiple compiled functions into one place for easier
replay. (MCH stands for "method context hive".)
3. Remove duplicates in the .MCH file. Many compiled functions are essentially
equivalent, such as trivial class constructors, or if some functions are
compiled multiple times in different scenarios or tests. We filter out the
duplicates, which makes playback much faster, and the resultant MCH file much
smaller. This can be done as part of the "merge" step.
4. Create a "clean" .MCH with no SuperPMI failures. The original collected MCH
file might not replay cleanly. This is generally due to existing, un-investigated
SuperPMI bugs or limitations. We don't want to see these during normal playback,
so filter out the failing replays at this point so the "baseline" replay is clean.
5. Create a table of contents (TOC) file. This creates an index for the generated
MCH file that greatly speeds up certain operations.
6. Test final .MCH file is clean. This is purely to check that the resultant
MCH file is in a good state for future use.


## Collect .MC files

Set the following environment variables:

```
SuperPMIShimLogPath=<full path to an existing, empty temporary directory>
SuperPMIShimPath=<full path to clrjit.dll, the "standalone" JIT>
COMPlus_JitName=superpmi-shim-collector.dll
```

for example, on Windows:

```
mkdir f:\spmi\temp
set SuperPMIShimLogPath=f:\spmi\temp
set SuperPMIShimPath=f:\gh\runtime\artifacts\tests\coreclr\windows.x64.Checked\Tests\Core_Root\clrjit.dll
set COMPlus_JitName=superpmi-shim-collector.dll
```

(On Linux, use `libclrjit.so` and `libsuperpmi-shim-collector.so`.
On Mac, use `libclrjit.dylib` and `libsuperpmi-shim-collector.dylib`.)

Note that the `superpmi-shim-collector.dll` must live in the same directory as the `coreclr.dll`
(or libcoreclr.so on Linux) that will be invoked when running .NET Core. This will normally be
a `Core_Root` directory, since you must create such a directory to be able to run
.NET Core applications (such as by using the `corerun` tool).

If you want to collect using an official .NET Core build, you will need to build a matching set
of superpmi binaries, and copy `superpmi-shim-collector.dll` to the correct directory. This
option has not been tested (as far as I know).

Then, cause the JIT to compile some code. Do one or more of:
1. Run a managed scenario.
2. Run managed code tests.
3. Crossgen some assemblies.
4. Run PMI over some assemblies (see https://github.com/dotnet/jitutils for details on PMI)

When done running programs, un-set these environment variables.

Now, you will have a large number of .MC files in the specified temporary
directory (specified by `SuperPMIShimLogPath`).


## Merge .MC files to .MCH file

Merge the generated .MC files using the `mcs` tool:

```
mcs -merge base.mch *.mc -recursive -dedup -thin
```

This assumes the current directory is the root directory where the .MC files
were placed, namely the directory specified as `SuperPMIShimLogPath` above.
You can also specify a directory prefix to the file system regular expression.
The `-recursive` flag is only necessary if .MC files also exist in subdirectories
of this, and you want those also added to the resultant, collected `base.mch`
file. The `-dedup` and `-thin` options remove duplicates as the files are merged,
and remove the normally-unused CompileResults (e.g., the code generated during
the initial collection).

So, for the example above, you might use:

```
f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\mcs.exe -merge f:\spmi\base.mch f:\spmi\temp\*.mc -recursive -dedup -thin
```

Note that `mcs -merge` without `-dedup -thin` is literally just a file concatenation
of many files into one, which would double the required disk space.

After this step, you can remove all the individual .MC files unless you want
to keep them to debug the SuperPMI collection process itself.


## Remove duplicates in the .MCH file

One benefit of SuperPMI is the ability to remove duplicated compilations, so
on replay only unique functions are compiled. If you didn't use the `-merge -dedup -thin`
option above, you can do the deduplication separately, using the following to create a
"unique" set of functions:

```
mcs -removeDup -thin base.mch unique.mch
```

Note that `-thin` is not required. However, it will delete all the compilation
results collected during the collection phase, which makes the resulting MCH
file smaller. Those compilation results are not required for playback for
the ways in which we normally use SuperPMI.

For the continuing example, you might use:

```
f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\mcs.exe -removeDup -thin f:\spmi\base.mch f:\spmi\unique.mch
```

After this step, you can remove the base.mch file (unless you want to debug
the SuperPMI collection process itself).


## Create a "clean" .MCH with no SuperPMI failures

As stated above, due to various bugs or otherwise uninvestigated issues, a SuperPMI
replay of the unique.mch file might contain errors. We don't want that, so we filter
out those errors in a "baseline" run, as follows.

(Note that if you are following the steps above, you most likely deduplicated
during the merge operation, so you have a `base.mch` file, not a `unique.mch` file.)

```
superpmi -p -f basefail.mcl unique.mch clrjit.dll
mcs.exe -strip basefail.mcl unique.mch final.mch
```

Or, continuing the example above, giving full paths, we have:

```
f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\superpmi.exe -p -f f:\spmi\basefail.mcl f:\spmi\unique.mch f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\clrjit.dll
f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\mcs.exe -strip f:\spmi\basefail.mcl f:\spmi\unique.mch f:\spmi\final.mch
```


## Create a table of contents (TOC) file

This is

```
mcs -toc final.mch
```

or, using the full paths from above:

```
f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\mcs.exe -toc f:\spmi\final.mch
```


## Test final .MCH file is clean

This is done using a replay just like for the "create clean .MCH file" step:

```
superpmi -p -f finalfail.mcl final.mch clrjit.dll
```

Or, continuing the example above, giving full paths, we have:

```
f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\superpmi.exe -p -f f:\spmi\finalfail.mcl f:\spmi\final.mch f:\gh\runtime\artifacts\bin\coreclr\windows.x64.Checked\clrjit.dll
```

In this case, if `finalfail.mcl` is not empty, there was a failure in the final "check" replay.
This will lead to the same failure in all future replays, if the resultant final.mch file is
published for general use. This is annoying, but not necessarily a fatal problem.
It's possible to cycle the process, stripping this failure from the final MCH,
testing for "clean-ness" again, and repeating. It should not be necessary, however.


## Publishing the SuperPMI collection

The files that should be published for consumption either by yourself or by a group
are `final.mch` and `final.mch.mct` (the TOC). All other files described above are
intermediate files in the collection process, and are not needed afterwards.


# Playback

Once you have a merged, de-duplicated MCH collection, you can play it back
using:

    superpmi -p final.mch clrjit.dll

The `-p` switch says to utilize all the processors on your machine,
and replay in parallel. You can omit this if you wish to replay
without using parallelism (this is usually done when replaying
a single compilation, whereas `-p` is usually used when replaying
a full MCH file set of compilations).
