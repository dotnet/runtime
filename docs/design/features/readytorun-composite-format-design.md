ReadyToRun File Format - Composite Format Design Notes
======================================================

Revisions:
* 1 - [Tomas Rylek](https://github.com/trylek) - 1/27/2020

# Introduction

The purpose of this document is to summarize the necessary format and code changes to enable
support for *Composite ReadyToRun Files* combining multiple input MSIL assemblies into a single
ReadyToRun executable.

# Traditional vs. composite R2R image

The logical goal of a composite R2R image is to have a single PE R2R binary represent a semantic
union of multiple input MSIL assemblies compiled as a unit by the R2R compiler to produce the
composite output.

Some motivations for producing composite R2R images include:

* Better performance: compiling multiple MSIL assemblies into a single output R2R executable
  creates an even tighter coupling than the large version bubble using separate files and the
  compiler and runtime can be more efficient (e.g. by using large memory pages).
* Easier deployment: desire to avoid having to deal with potentially hundreds or thousands of
  assemblies.
* Side-by-side deployment: a self-contained app doesn't need to care about the location of its
  dependencies, their proper versions, about sharing them with other apps (including other
  versions of the same app).

Semantic requirements for the composite R2R format flow from the combination of the basic design
principle that the R2R file is a code cache to save jitting time and from the fact that the
tighter coupling can be utilized to improve codegen quality.

## R2R file is a code cache

A corollary of this basic principle is that the composite file must maintain full functionality
of single-input R2R files w.r.t. things like reflection, dynamic or generics. To achieve this
goal the composite R2R file must have access to the original MSIL metadata for all input
assemblies represented by the output R2R file. We propose supporting two different configurations
for this model:

* Standalone MSIL - MSIL metadata and IL streams (the original managed assemblies) remain on
disk as separate files next to the compiled R2R executable;
* Embedded MSIL - metadata and IL are embedded as blobs in the compiled R2R executable so that
the original managed assembly files are not needed at runtime.

This is a substantial deviation from the previous format that only had one COR header
addressed by the COM data directory entry in the COFF header. To achieve this goal,
we propose using two complementary strategies:

* In the composite R2R file with embedded metadata, there must be a new table of COR headers
and metadata blobs representing the MSIL metadata from all the input assemblies. The table
must be indexable by simple assembly name for fast lookup.

* in contrast to managed assemblies and single-input R2R executables, composite R2R files
  don't expose any COR header (it's not meaningful as the file potentially contains a larger
  number of IL files).

**Note:** As of this proposal we're not addressing managed C++ code including both native and
managed code. For pre-built native code, it's generally not possible to relocate RVA's as the
fixup table information is not rich enough. As a consequence, it's simply not possible to roll
multiple managed C++ modules containing native code into a single output R2R PE binary. We might
be able to consider some mitigations like allowing a single managed binary containing native
code in a composite build but I'm not sure whether they are worth the effort and desirable
in general.

# Structural format changes specific to composite build mode

When a R2R file has the `READYTORUN_FLAG_COMPOSITE` bit set in the R2R header, it conforms to
the composite file format. The main differences between composite and single-input R2R PE files
are summarized below.

## COFF, PE and metadata headers

In single-input R2R PE files the COFF header (the COM descriptor directory entry) points at the
COR header of the input MSIL metadata which then points at the R2R header (via the
`ManagedNativeHeaderDirectory` field).

In composite R2R files there is no global COR header and the R2R header is located through the
well-known export symbol `RTR_HEADER`. The "actual" input assemblies are tracked under the new
R2R header table `READYTORUN_SECTION_ASSEMBLIES`.

## Manifest metadata and component assembly table

In single-input R2R PE files compiled in the large version bubble mode, the metadata manifest
contains the set of assemblies containing functions called from the input assembly (possibly
with some transitivity due to inlining); in composite R2R PE files the manifest metadata
contains all assemblies contained within the composite file. The linear indices of the
assemblies in the manifest metadata AssemblyRef table correspond to indexing within the
`READYTORUN_SECTION_ASSEMBLIES` table.

**Disclaimer:** The manifest metadata is a new feature that hasn't shipped yet; it involves
straightforward adaptation of a fragile NGen technology to ReadyToRun images as an expedite
means for enabling new functionality (larger version bubble support). The precise details of
this encoding are still work in progress and likely to further evolve.

**Note**: as of now it's unclear whether we need to support a hybrid scenario where the large
version bubble is represented by an arbitrary mixture of single-input and composite R2R files.
If that is the case, manifest metadata would need to be decoupled from the index to
`READYTORUN_SECTION_ASSEMBLIES`.

Alternatively we could make it such that `READYTORUN_SECTION_MANIFEST_METADATA` holds all
component assemblies of the current composite image at the beginning of the AssemblyRef table
followed by the other needed assemblies *within the version bubble outside of the current
composite image* - `READYTORUN_SECTION_ASSEMBLIES` would then contain fewer rows than the
AssemblyRef table in the manifest metadata, corresponding to the initial rows for the
assemblies within the composite image.

## Per-assembly tables

In single-input R2R PE files the R2R section `READYTORUN_SECTION_AVAILABLE_TYPES` encodes
a native hashtable of types within a given assembly. In composite R2R files, the available
types need to be split per input assembly and are referenced from `READYTORUN_SECTION_ASSEMBLIES`.

In single-input R2R PE files the R2R section `READYTORUN_SECTION_METHOD_ENTRYPOINTS` encodes
a native hashtable of methods within a given assembly. In composite R2R files, the method
entrypoints need to be split per input assembly and are referenced from
`READYTORUN_SECTION_ASSEMBLIES`.

## Instance entrypoint table

In single-input R2R PE files the  generic instantiations generated for the input MSIL assembly
are placed in `READYTORUN_SECTION_INSTANCE_METHOD_ENTRYPOINTS`. In composite R2R files, this
section represents all instance entrypoints emitted within the composite build (i.e. generic
instantiations needed by any of the input assemblies). CoreCLR runtime requires changes to
properly look up methods stored in this section in the composite R2R case.

# CoreCLR runtime changes

CoreCLR runtime will need to become able to recognize the new composite R2R format by means
of locating the well-known export `RTR_HEADER` and validating the ReadyToRun header (magic
constant, version number and the `READYTORUN_FLAG_COMPOSITE` flag) and behave accordingly:

* For composite files with embedded MSIL, we shouldn't need MVID checks for reference
  assemblies within the tight version bubble represented by the single file.
* We need to improve generic instantiation lookup algorithm to be able to locate all
  instantiations emitted into the composite file.
* CoreCLR runtime will need to parse the manifest metadata (and possibly the
  `READYTORUN_SECTION_SIMPLE_ASSEMBLY_NAME_LOOKUP` section) and set up lookup structures to
  facilitate fast assembly lookup for available types and method entrypoints.
* The runtime will need to start consulting the `READYTORUN_SECTION_ASSEMBLIES` table to locate
  MSIL metadata within the composite executable - this logic should definitely take precedence
  to arbitrary files on the disk to satisfy consistency and security guarantees.

# Tooling changes

## R2RDump

R2R dump will require fixing to support multiple ECMA metadata blobs in the composite R2R file,
the new header sections and new ways of manipulating the available types and method entrypoint
tables. We may temporarily consider adding some "minimalistic ILDASM" functionality to R2R dump
in case it turns out to be much more involved to patch ILDASM itself to support the new format -
at the very least the functionality to extract the ECMA-335 metadata blobs into separate files
that could be subsequently opened by ILDASM or ILSpy.

## ILDASM / ILSpy

Ideally we should patch ILDASM / ILSpy to cleanly handle the composite R2R file format; sadly this may
end up being a relatively complex change due to the presence of multiple MSIL metadata blocks in the
file.

# Required diagnostic changes

Active and passive diagnostics (live and dump debugging) will require new debugger support to
understand the composite file, access the ECMA-335 metadata blocks and understand their
correlation to the compiled R2R code. Additional changes will be likely needed to support
profiling. Design discussions with the diagnostic team are only just starting so this is
currently a big **TODO**.
