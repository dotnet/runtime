# Documentation for the superpmi.py tool

SuperPMI is a tool for developing and testing the JIT compiler.
General information on SuperPMI can be found [here](../tools/superpmi/readme.md).

## Overview

superpmi.py is a tool to simplify the use of SuperPMI.
The tool has three primary modes: collect, replay, and asmdiffs.
Below you will find more specific information on each of the different modes.

superpmi.py lives in the dotnet/runtime GitHub repo, src\coreclr\scripts directory.

## General usage

From the usage message:

```
usage: superpmi.py [-h] {collect,replay,asmdiffs,upload,download,list-collections,merge-mch} ...

Script to run SuperPMI replay, ASM diffs, and collections. The script also manages the Azure
store of pre-created SuperPMI collection files. Help for each individual command can be shown
by asking for help on the individual command, for example `superpmi.py collect --help`.

positional arguments:
  {collect,replay,asmdiffs,upload,download,list-collections,merge-mch}
                        Command to invoke

optional arguments:
  -h, --help            show this help message and exit
```

## Replay

The simplest usage is to replay using:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py replay
```

In this case, everything needed is found using defaults:

- The processor architecture is assumed to be the current default (e.g., x64).
- The build type is assumed to be Checked.
- Core_Root is found by assuming superpmi.py is in the normal location in the
clone of the repo, and using the processor architecture, build type, and current
OS, to find it in the default `artifacts` directory location. Note that you must
have performed a product build for this platform / build type combination, and
created the appropriate Core_Root directory as well.
- The SuperPMI tool and JIT to use for replay is found in Core_Root.
- The SuperPMI collections to use for replay are found in the Azure store of
precomputed collections for this JIT-EE interface GUID, OS, and processor architecture.

If you want to use a specific MCH file collection, use the `-mch_files` argument to specify
one or more MCH files on your machine:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py replay -mch_files f:\spmi\collections\tests.pmi.windows.x64.Release.mch
```

The `-mch_files` argument takes a list of one or more directories or files to use. For
each directory, all the MCH files in that directory are used.

If you want to use just a subset of the collections, either default collections or collections
specified by `-mch_files`, use the `-filter` argument to restrict the MCH files used, e.g.:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py replay -filter tests
```

## ASM diffs

To generate ASM diffs, use the `asmdiffs` command. This requires a "diff" and "baseline"
JIT. By default, the "diff" JIT is determined automatically as for the "replay" command,
described above. Also by default, the baseline JIT is determined based on comparing the
state of your branch with the `main` branch, and downloading an appropriate baseline JIT from the JIT
rolling build system. Alternatively, you can specify the path to a baseline JIT
compiler using the `-base_jit_path` argument.

Example:
```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py asmdiffs
```

ASM diffs requires the coredistools library. The script attempts to find
or download an appropriate version that can be used.

As for the "replay" case, the set of collections used defaults to the set available
in Azure, or can be specified using the `mch_files` argument. In either case, the
`-filter` argument can restrict the set used.

## Collections

SuperPMI requires a collection to enable replay. You can do a collection
yourself using the superpmi.py `collect` command, but it is more convenient
to use existing precomputed collections stored in Azure.

You can see which collections are available for your current settings using
the `list-collections` command. You can also see all the available collections
using the `list-collections --all` command. Finally, you can see which Azure stored
collections have been locally cached on your machine in the default cache location
by using `list-collections --local`.

Note that when collections are downloaded, they are cached locally. If there are
any cached collections, then no download attempt is made. To force re-download,
use the `--force_download` argument to the `replay`, `asmdiffs`, or `download` command.

### Creating a collection

Example commands to create a collection (on Linux, by running the tests):

```
# First, build the product, possibly the tests, and create a Core_Root directory.
/Users/jashoo/runtime/src/coreclr/scripts/superpmi.py collect bash "/Users/jashoo/runtime/src/tests/runtest.sh x64 checked"
```

The above command collects over all of the managed code called by the
child process. Note that this allows many different invocations of any
managed code.

You can also collect using PMI instead of running code. Do with with the `--pmi` and `-assemblies`
arguments. E.g.:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py collect --pmi -assemblies f:\assembly_store -output_mch_path f:\collections\my_collection.mch
```

You can alternatively collect by running `crossgen` on a set of assemblies, e.g.:

```
python f:\gh\runtime\src\coreclr\scripts\superpmi.py collect --crossgen -assemblies f:\assembly_store -output_mch_path f:\collections\my_collection.mch
```

You can specify both `--pmi` and `--crossgen` to do both types of collections across the
same set of specified assemblies, with the results being accumulated into a single collection.

Note that the collection process generates gigabytes of data. Most of this data will
be removed when the collection is finished. It is recommended to set the TEMP variable
to a location with adequate space, and preferably on a fast SSD to improve performance,
before running `collect` to avoid running out of disk space.

### Azure Storage collections

As stated above, you can use the `list-collections` command to see which collections
are available in Azure.

There is also a `download` command to download one or more Azure stored collection
to the local cache, as well as an `upload` command to populate the Azure collection
set.
