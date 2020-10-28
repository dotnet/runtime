set(coreclr_root "../../../coreclr")

include_directories(
  ${coreclr_root}/src/gc
  ${coreclr_root}/src/gc/env
  ${coreclr_root}/src/gc/unix) 

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
	gcscan.h)

addprefix(mono_coregc_sources ../coregc/ "${mono_coregc_sources_base}")
addprefix(coreclr_coregc_sources ${coreclr_root}/src/gc "${coregc_sources_base}")
set(coregc_sources ${mono_coregc_sources} ${coreclr_coregc_sources})

# message(FATAL_ERROR "!!! ${coregc_sources} !!!\n")

set_source_files_properties(${coregc_sources} PROPERTIES COMPILE_DEFINITIONS "HAVE_CORE_GC")
