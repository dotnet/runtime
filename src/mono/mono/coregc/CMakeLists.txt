
set(mono_coregc_sources_base
    coregc-mono.cpp
    coregc-mono.h
 )

 set (coregc_sources_base
    gceventstatus.h
	gcconfig.cpp
	gcconfig.h
	gccommon.cpp
	gcsvr.cpp
	gcwks.cpp
	handletable.cpp
	handletable.h
	handletablepriv.h
	handletablecore.cpp
	handletablescan.cpp
	objecthandle.cpp
	objecthandle.h
	softwarewritewatch.cpp
	softwarewritewatch.h
	gchandletable.cpp
	gchandletableimpl.h
	gceesvr.cpp
	gceewks.cpp
	gcload.cpp
	handletablecache.cpp
	unix/gcenv.unix.cpp
	unix/events.cpp
	unix/cgroup.cpp
	unix/cgroup.h
	gcscan.cpp
	$(coreclr_srcdir)/src/gc/gcscan.h)

addprefix(mono_coregc_sources_base ../coregc/ "${sgen_sources_base}")
addprefix(coregc_sources_base, $(top_srcdir)/../coreclr/src/gc)
set_source_files_properties(${sgen_sources} PROPERTIES COMPILE_DEFINITIONS "HAVE_SGEN_GC")
