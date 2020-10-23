
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

addprefix(coregc_sources m ../coregc/ "${ono_coregc_sources_base}")
# addprefix(coregc_sources coregc_sources_base $(top_srcdir)/../coreclr/src/gc)
set_source_files_properties(${coregc_sources} PROPERTIES COMPILE_DEFINITIONS "HAVE_CORE_GC")
