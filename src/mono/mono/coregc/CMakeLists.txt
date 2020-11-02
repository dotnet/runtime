set(coreclr_root "../../../coreclr")

include_directories(
  ../../../mono
  ../../../mono/mono
  ../../../mono/mono/coregc
  ${coreclr_root}/src/gc
  ${coreclr_root}/src/gc/env
  ${coreclr_root}/src/gc/unix
  ${coreclr_root}/src/pal/inc/rt
  ${coreclr_root}/src/pal/inc)


add_definitions(-D_AMD64_ -DBIT64 -DFEATURE_REDHAWK -DPLATFORM_UNIX -DHOST_64BIT -DHOST_AMD64 -DTARGET_UNIX -DNDEBUG -DHAVE_CLOCK_GETTIME_NSEC_NP -DHAVE_SYSCTL)

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
