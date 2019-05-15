# Introduction #

Core dump generation on Linux and other non-Windows platforms has several challenges. Dumps can be very large and the default name/location of a dump is not consistent across all our supported platforms.  The size of a full core dumps can be controlled somewhat with the "coredump_filter" file/flags but even with the smallest settings may be still too large and may not contain all the managed state needed for debugging. By default, some platforms use _core_ as the name and place the core dump in the current directory from where the program is launched; others add the _pid_ to the name. Configuring the core name and location requires superuser permission. Requiring superuser to make this consistent is not a satisfactory option.

Our goal is to generate core dumps that are on par with WER (Windows Error Reporting) crash dumps on any supported Linux platform. To the very least we want to enable the following: 
- automatic generation of minimal size minidumps. The quality and quantity of the information contained in the dump should be on par with the information contained in a traditional Windows mini-dump.
- simple configurabilty by the user (not _su_!). 

Our solution at this time is to intercept any unhandled exception in the PAL layer of the runtime and have coreclr itself trigger and generate a "mini" core dump. 

# Design #

We looked at the existing technologies like Breakpad and its derivatives (e.g.: an internal MS version called _msbreakpad_ from the SQL team....). Breakpad generates Windows minidumps but they are not compatible with existing tools like Windbg, etc. Msbreakpad even more so. There is a minidump to Linux core conversion utility but it seems like a wasted extra step. _Breakpad_ does allow the minidump to be generated in-process inside the signal handlers. It restricts the APIs to what was allowed in a "async" signal handler (like SIGSEGV) and has a small subset of the C++ runtime that was also similarly constrained. We also need to add the set of memory regions for the "managed" state which requires loading and using the _DAC_'s (*) enumerate memory interfaces. Loading modules is not allowed in an async signal handler but forking/execve is allowed so launching an utility that loads the _DAC_, enumerates the list of memory regions and writes the dump is the only reasonable option. It would also allow uploading the dump to a server too.

\* The _DAC_ is a special build of parts of the coreclr runtime that allows inspection of the runtime's managed state (stacks, variables, GC state heaps) out of context. One of the many interfaces it provides is [ICLRDataEnumMemoryRegions](https://github.com/dotnet/coreclr/blob/master/src/debug/daccess/dacimpl.h) which enumerates all the managed state a minidump would require to enable a fuitful debugging experience.

_Breakpad_ could have still been used out of context in the generation utility but there seemed no value to their Windows-like minidump format when it would have to be converted to the native Linux core format away because in most scenarios using the platform tools like _lldb_ is necessary. It also adds a coreclr build dependency on Google's _Breakpad_ or SQL's _msbreakpad_ source repo. The only advantage is that the breakpad minidumps may be a little smaller because minidumps memory regions are byte granule and Linux core memory regions need to be page granule.

# Implementation Details #

### Linux ###

Core dump generation is triggered anytime coreclr is going to abort (via [PROCAbort()](https://github.com/dotnet/coreclr/blob/master/src/pal/src/include/pal/process.h)) the process because of an unhandled managed exception or an async signal like SIGSEGV, SIGILL, SIGFPE, etc. The _createdump_ utility is located in the same directory as libcoreclr.so and is launched with fork/execve. The child _createdump_ process is given permission to ptrace and access to the various special /proc files of the crashing process which waits until _createdump_ finishes.

The _createdump_ utility starts by using ptrace to enumerate and suspend all the threads in the target process. The process and thread info (status, registers, etc.) is gathered. The auxv entries and _DSO_ info is enumerated. _DSO_ is the in memory data structures that described the shared modules loaded by the target. This memory is needed in the dump by gdb and lldb to enumerate the shared modules loaded and access their symbols. The module memory mappings are gathered from /proc/$pid/maps. None of the program or shared modules memory regions are explicitly added to dump's memory regions. The _DAC_ is loaded and the enumerate memory region interfaces are used to build the memory regions list just like on Windows. The threads stacks and one page of code around the IP are added. The byte sized regions are rounded up to pages and then combined into contagious regions.

All the memory mappings from /proc/$pid/maps are in the PT_LOAD sections even though the memory is not actually in the dump. They have a file offset/size of 0. 

After all the process crash information has been gathered, the ELF core dump with written. The main ELF header created and written. The PT\_LOAD note section is written one entry for each memory region in the dump. The process info, auxv data and NT_FILE entries are written to core. The NT\_FILE entries are built from module memory mappings from /proc/$pid/maps. The threads state and registers are then written. Lastly all the memory regions gather above by the _DAC_, etc. are read from the target process and written to the core dump. All the threads in the target process are resumed and _createdump_ terminates.

**Severe memory corruption**

As long as control can making it to the signal/abort handler and the fork/execve of the utility succeeds then the _DAC_ memory enumeration interfaces can handle corruption to a point; the resulting dump just may not have enough managed state to be useful. We could investigate detecting this case and writing a full core dump.

**Stack overflow exception**

Like the severe memory corruption case, if the signal handler (`SIGSEGV`) gets control it can detect most stack overflow cases and does trigger a core dump. There are still many cases where this doesn't happen and the OS just terminates the process. There is a bug in the earlier versions (2.1.x or less) of the runtime where _createdump_ isn't invoked for any stack overflow.

### FreeBSD/OpenBSD/NetBSD ###

There will be some differences gathering the crash information but these platforms still use ELF format core dumps so that part of the utility should be much different. The mechanism used for Linux to give _createdump_ permission to use ptrace and access the /proc doesn't exists on these platforms.

### OS X ###

Gathering the crash information on OS X will be quite a bit different than Linux and the core dump will be written in the Mach-O format instead of ELF. The OS X support currently has not been implemented.

# Configuration/Policy #

NOTE: Core dump generation in docker containers require the ptrace capability (--cap-add=SYS_PTRACE or --privileged run/exec options).

Any configuration or policy is set with environment variables which are passed as options to the _createdump_ utility.

Environment variables supported:

- `COMPlus_DbgEnableMiniDump`: if set to "1", enables this core dump generation. The default is NOT to generate a dump.
- `COMPlus_DbgMiniDumpType`: See below. Default is "2" MiniDumpWithPrivateReadWriteMemory.
- `COMPlus_DbgMiniDumpName`: if set, use as the template to create the dump path and file name. The pid can be placed in the name with %d. The default is _/tmp/coredump.%d_.
- `COMPlus_CreateDumpDiagnostics`: if set to "1", enables the _createdump_ utilities diagnostic messages (TRACE macro).

COMPlus_DbgMiniDumpType values:


|Value|Minidump Enum|Description|
|-|:----------:|----------|
|1| MiniDumpNormal                               | Include just the information necessary to capture stack traces for all existing threads in a process. Limited GC heap memory and information. |
|2| MiniDumpWithPrivateReadWriteMemory (default) | Includes the GC heaps and information necessary to capture stack traces for all existing threads in a process. |
|3| MiniDumpFilterTriage                         | Include just the information necessary to capture stack traces for all existing threads in a process. Limited GC heap memory and information. |
|4| MiniDumpWithFullMemory                       | Include all accessible memory in the process. The raw memory data is included at the end, so that the initial structures can be mapped directly without the raw memory information. This option can result in a very large file. |

(Please refer to MSDN for the meaning of the [minidump enum values](https://msdn.microsoft.com/en-us/library/windows/desktop/ms680519(v=vs.85).aspx) reported above)

**Command Line Usage**

The createdump utility can also be run from the command line on arbitrary .NET Core processes. The type of dump can be controlled with the below command switches. The default is a "minidump" which contains the majority the memory and managed state needed. Unless you have ptrace (CAP_SYS_PTRACE) administrative privilege, you need to run with sudo or su. The same as if you were attaching with lldb or other native debugger.

`sudo createdump <pid>`  

    createdump [options] pid
    -f, --name - dump path and file name. The pid can be placed in the name with %d. The default is "/tmp/coredump.%d"
    -n, --normal - create minidump (default).
    -h, --withheap - create minidump with heap.
    -t, --triage - create triage minidump.
    -u, --full - create full core dump.
    -d, --diag - enable diagnostic messages.

# Testing #

The test plan is to modify the SOS tests in the (still) private debuggertests repo to trigger and use the core minidumps generated. Debugging managed core dumps on Linux is not supported by _mdbg_ at this time until we have a ELF core dump reader so only the SOS tests (which use _lldb_ on Linux) will be modified.

# Open Issues #

- May need more than just the pid for decorating dump names for docker containers because I think the _pid_ is always 1.
- Do we need all the memory mappings from `/proc/$pid/maps` in the PT\_LOAD sections even though the memory is not actually in the dump? They have a file offset/size of 0. Full dumps generated by the system or _gdb_ do have these un-backed regions.
- There is no way to get the signal number, etc. that causes the abort from the _createdump_ utility using _ptrace_ or a /proc file. It would have to be passed from CoreCLR on the command line.
- Do we need the "dynamic" sections of each shared module in the core dump? It is part of the "link_map" entry enumerated when gathering the _DSO_ information.
- There may be more versioning and/or build id information needed to be added to the dump.
- It is unclear exactly what cases stack overflow does not get control in the signal handler and when the OS just aborts the process.
