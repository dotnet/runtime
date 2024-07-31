# IL Linker

The IL Linker is a tool one can use to only ship the minimal possible set of
functions that a set of programs might require to run as opposed to the full
libraries.

## How does the IL Linker work?

The IL Linker tool analyses the intermediate code (CIL) produced by every compiler
targeting the .NET platform like mcs, csc, vbnc, booc or others. It will walk
through all the code that it is given to it, and basically, perform a mark and
sweep operations on all the code that it is referenced, to only keep what is
necessary for the source program to run.

For more details about how the IL Linker tool works and other documentation please
check [docs/](/docs/tools/illink/README.md) folder.
