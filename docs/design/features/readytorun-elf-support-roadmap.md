Roadmap to native Linux executable format (ELF) support in Ready to Run
=======================================================================

Revisions:
* 1.1 - [Tomas Rylek](https://github.com/trylek) - 2/27/2020

# Introduction

This document details the steps needed for supporting native Linux executable
format (ELF) for ready-to-run binaries on Linux. Today, ready-to-run binaries
use the Windows PE executable format on all three supported platforms
(Windows, Linux, OSX). Due to this fact ready-to-run binaries are "unnatural"
on non-Windows platforms incurring both performance and user experience costs
(incomplete integration with OS-native application management, inability to
use OS executable loader, lack of OS-native diagnostic experience). This
proposal tries to summarize the steps needed towards native executable support.

# Scope of changes

Support for Linux native executable format will require changes to the following
areas:

* **CoreCLR runtime**: ideally we should be able to use the Linux ELF headers
  in the native code. The current treatment of ready-to-run files involving
  classes PEFile, PEImage and PEImageLayout aka PEDecoder, need virtualizing
  to allow for their ELF counterparts and proper deduplication of common logic
  especially related to R2R that should be mostly autonomous once the R2R header
  has been located. Fully switching over to native ELF format for ready-to-run
  binaries is more complicated as it involves not yet implemented embedded MSIL
  in native R2R binaries, a new concept we're currently developing as part of
  the composite R2R effort; this will replace the existing lookup for COR header
  using the COM descriptor directory entry in the PE header.

* **Crossgen2 compiler**: The SectionBuilder and R2RPEBuilder needs to be
  decoupled from the PE format by some form of virtualization. We need to
  port the various ELF header structures to managed code for use in Crossgen2
  and R2RDump (do we need some approval or special arrangements for porting
  the Linux header files like elf.h and elftypes.h to managed code in the runtime
  repo?). After we have the constants, structures and their serialization
  mechanisms in place and after we figure out the proper abstraction the work
  should be pretty straightforward in terms of just filling in the various header
  and section tables and emitting the section data in the proper place.

* **Diagnostics / SOS**: switching over to native ELF will have fundamental effect on
  diagnostics. While the overall long-term effect is assumed to be positive,
  it will take time to bridge the user experience gap. It is likely that for some
  temporary period of time we'll need to keep support for both PE files and ELF
  files on Linux until all diagnostic functionality for ELF has been put on par
  with pre-existing diagnostic support for PE-based executables.

* **Visual Studio, VS Code**: switching over the executable format is certain to
  have non-trivial effect on both VS and VS Code. We need to plan ahead with the
  relevant teams to make sure we cover all the relevant project template, build
  system, debugger, profiler and other aspects.

* **R2RDump**: The PE header reader that is today using the managed assembly
  System.Reflection.PortableExecutable needs to be generalized to recognize and
  parse the ELF headers and locate the RTR_HEADER export symbol - once we're able
  to identify the R2R header in the file, we immediately have access to all the
  sections and data. Naturally we'll also need to adjust the current logic for
  translating RVA's to file offsets.

* **ILSpy**: will hopefully continually ingest R2RDump changes so it should
  automatically pick up support for ELF-embedded MSIL/ECMA-335 metadata.

# Proposed sequencing

The natural sequencing and initial T-shirt costing implied by the various
inter-dependencies and by the natural requirement that each step needs to be
verifiable / diagnosable is as follows:

* Import (rewrite) the relevant Linux headers elf.h and elftypes.h to managed
  code, probably somewhere under *src/coreclr/src/tools/Common/Internal/Runtime*.
  We need to verify licensing conditions for doing this. The actual work is
  junior level, estimated at 2 man-weeks including some rudimentary regression
  testing - e.g. writing a simple test that will dump a subset of the headers
  for a hard-coded ELF file used as a resource by the test.
  
  An interesting observation is that most ELF header structures have
  paraller 32-bit and 64-bit variants. It might be useful to logically unify them
  by internally using longs and just have virtual de-serializers that would have
  something like ReadNativeInt representing 32-bit or 64-bit integer based on
  the bitness.
  
  We probably don't need to support ELF to the fullest, e.g. endianness doesn't
  matter as our JIT and runtime doesn't support any high-endian CPU's (ARM has
  technically switchable endianness but we only support the little variant).

* Create a simplified counterpart of System.Reflection.PortableExecutable to
  support ELF, again somewhere under *src/coreclr/src/tools/Common/Internal/Runtime*.
  At some point we may consider porting it to libraries but initially it's better
  to keep that separate.
  
  We should also double-check whether mono doesn't already have something in this
  vein we could reuse or leverage. We basically need to be able to read / write
  the main ELF header, the program headers, the section headers and the symbols.
  This is junior level task estimated at 3 man-weeks including creating a validation
  test that should use the designed API to rewrite an arbitrary ELF executable and
  verify that existing tools like readelf properly parse the file.
  
  It doesn't need to have 100% fidelity but we need to verify it's not distorted
  and that the main components like targeting OS / architecture, pointers to the
  sections and symbols are kept intact. The transcript from the dumping tool
  should be made part of the output from the regression test in order to make
  potential regressions easy to diagnose.
  
  We should also try to figure out whether we might be able to implement the
  section builder logic in a similar manner for both PE and ELF builder - this
  would probably require creating a wrapper over the PE builder but it would be
  still easier than having two completely different code paths for composing the
  sections. We'll need to address minute differences like alignment considerations
  as on Linux **file offset - RVA** deltas need to be page-aligned.

* Virtualizing R2RDump header parsing to add ELF support. This should be
  junior-level task estimated at 1 man-week of work. It should check for the
  well-known signature magic values used by PE / ELF files to identify the format
  of the input file (both for the "main" and reference input files) and use
  proper logic to locate the R2R header, COR header / MetadatReader and the logic
  to map RVA's to file offsets.

* Virtualizing Crossgen2 SectionBuilder / R2RPEBuilder: this is senior / principal
  work estimated at 3 man-weeks to refactor section building to be independent on
  the choice of PE / ELF format and use the newly created ELF API to [re]construct
  the necessary headers and create a fully functional ELF executable.

  At this point this will probably still be just a dynamic library, not a
  full-fledged EXE. The full-fledged EXE should come in parallel with the
  single-exe effort as it basically involves emitting some host bootstrapper into
  the executable. Without the bootstrapper we can happily continue doing more or
  less what we're doing today just with slightly different logic for locating
  the headers.

* Implementing CoreCLR runtime counterpart of PEFile and PEImage for ELF and
  adding Linux-specific format detection to choose between PE and ELF as it's
  expected that for a certain period of time we'll need to support both formats
  on Linux. This is a senior / principal-level task estimated at 4 man-weeks
  of work.
  
  It will require initial investigation as to what native code we can reuse
  from the existing Linux libraries. We should consider some level of
  virtualization to cater for common constructs. The new objects should be
  Linux-specific and should use the Linux-native functions for manipulating
  executables like **dlopen** / **dlsym**.
  
  This will wildly differ from the existing PE manipulation, hopefully
  in the direction of simplifying it. [For single-exe aka R2R with a
  bootstrapper embedded in an ELF executable file, this will require
  additional design - likely the boostrapper itself should pass the
  RTR_HEADER symbol to the host.]

  Ultimately this should let us interchangeably open a PE / ELF on Linux and
  use both as the source of MSIL and native code. We should be able to verify
  that running Crossgen2 in both modes (PE vs. ELF output) produces identical
  test results on Linux.

* Once this initial work has been done, it should be possible to run CoreCLR
  R2R testing in ELF mode and use it to drive CQ and fix the remaining
  deficiencies. The work can also fan out to downstream efforts like support
  in the build scripts, VS integration, diagnostic experience and fixing
  additional utilities like ILSpy to support the new format.

# Initial Cost Calculation

At this point we're only attempting to calculate the estimated cost of known
bring-up items i.e. not including downstream items like VS integration,
diagnostics etc. Based on the described above work items the initial part
of the work amounts to:

* 6 man-weeks of junior-level work
* 7 man-weeks of senior / principal-level work
