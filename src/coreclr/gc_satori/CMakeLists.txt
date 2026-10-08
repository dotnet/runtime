set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(FEATURE_STANDALONE_GC)
  add_definitions(-DFEATURE_STANDALONE_GC)
  remove_definitions(-DSTRESS_HEAP)
  remove_definitions(-DWRITE_BARRIER_CHECK)
endif(FEATURE_STANDALONE_GC)

add_definitions(-DFEATURE_SVR_GC)
add_definitions(-DFEATURE_SATORI_GC)
add_definitions(-DFEATURE_SATORI_EXTERNAL_OBJECTS)

set(GC_SOURCES
  ../gc/gceventstatus.cpp
  ../gc/gcconfig.cpp
  ../gc/gccommon.cpp
  ../gc/gcscan.cpp
  ../gc/handletable.cpp
  ../gc/handletablecore.cpp
  ../gc/handletablescan.cpp
  ../gc/objecthandle.cpp
  ../gc/gchandletable.cpp
  ../gc/gcload.cpp
  ../gc/gcbridge.cpp
  ../gc/handletablecache.cpp
  SatoriGC.cpp
  SatoriHandlePartitioner.cpp
  SatoriHeap.cpp
  SatoriPage.cpp
  SatoriRegion.cpp
  SatoriObject.cpp
  SatoriRegionQueue.cpp
  SatoriAllocator.cpp
  SatoriRecycler.cpp
  SatoriTrimmer.cpp
  SatoriFinalizationQueue.cpp
  SatoriAllocationContext.cpp
  SatoriUtil.cpp
  SatoriLock.cpp
  SatoriWorkList.cpp
  SatoriGate.cpp
)

include_directories(../gc)
include_directories(../pal/inc)

if (CLR_CMAKE_TARGET_WIN32)
  set(GC_WRITE_BARRIER_SOURCES
    ${ARCH_SOURCES_DIR}/writebarrier.asm
  )

  set(GC_HEADERS
    ../gc/env/common.h
    ../gc/env/etmdummy.h
    ../gc/env/gcenv.base.h
    ../gc/env/gcenv.ee.h
    ../gc/env/gcenv.h
    ../gc/env/gcenv.interlocked.h
    ../gc/env/gcenv.interlocked.inl
    ../gc/env/gcenv.object.h
    ../gc/env/gcenv.os.h
    ../gc/env/gcenv.structs.h
    ../gc/env/gcenv.sync.h
    ../gc/env/gcenv.windows.inl
    ../gc/env/volatile.h
    ../gc/gc.h
    ../gc/gcinternal.h
    ../gc/gcconfig.h
    ../gc/gcbridge.h
    ../gc/gcdesc.h
    ../gc/gcenv.ee.standalone.inl
    ../gc/gcenv.inl
    ../gc/gcevent_serializers.h
    ../gc/gcevents.h
    ../gc/gceventstatus.h
    ../gc/gchandletableimpl.h
    ../gc/gcimpl.h
    ../gc/gcinterface.dac.h
    ../gc/gcinterface.ee.h
    ../gc/gcinterface.h
    ../gc/gcpriv.h
    ../gc/gcrecord.h
    ../gc/gcscan.h
    ../gc/handletable.h
    ../gc/handletable.inl
    ../gc/handletablepriv.h
    ../gc/objecthandle.h
    ../gc/softwarewritewatch.h
    SatoriGC.h
    SatoriHandlePartitioner.h
    SatoriHeap.h
    SatoriPage.h
    SatoriRegion.h
    SatoriRegion.inl
    SatoriObject.h
    SatoriObject.inl
    SatoriQueue.h
    SatoriRegionQueue.h
    SatoriAllocator.h
    SatoriRecycler.h
    SatoriTrimmer.h
    SatoriFinalizationQueue.h
    SatoriWorkChunk.h
    SatoriWorkList.h
    SatoriAllocationContext.h
    SatoriUtil.h
    SatoriLock.h
    SatoriGate.h
  )
else()
  set(GC_WRITE_BARRIER_SOURCES
    ${ARCH_SOURCES_DIR}/writebarrier.S
  )
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_HOST_WIN32)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    Synchronization.lib
    advapi32.lib
    coreclrminipal)
endif(CLR_CMAKE_HOST_WIN32)

set (GC_LINK_LIBRARIES
    ${GC_LINK_LIBRARIES}
    gc_pal
    minipal)

list(APPEND GC_SOURCES ${GC_HEADERS})

convert_to_absolute_path(GC_SOURCES ${GC_SOURCES})

if (CLR_CMAKE_TARGET_WIN32)
  if (CLR_CMAKE_HOST_ARCH_ARM64)
    # The Microsoft ARM64 assembler does not have a built-in C preprocessor,
    # unlike ml64, so the asm sources need to be preprocessed first.
    include_directories(
      ${ARCH_SOURCES_DIR}
      ../vm/${ARCH_SOURCES_DIR}
      ${CMAKE_CURRENT_BINARY_DIR}/../vm/wks)
    convert_to_absolute_path(GC_WRITE_BARRIER_SOURCES ${GC_WRITE_BARRIER_SOURCES})
    preprocess_files(GC_WRITE_BARRIER_SOURCES ${GC_WRITE_BARRIER_SOURCES})
  endif()
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(satori_writebarrier OBJECT ${GC_WRITE_BARRIER_SOURCES})
target_include_directories(satori_writebarrier PRIVATE
  ${ARCH_SOURCES_DIR}
  ../vm/${ARCH_SOURCES_DIR}
  ${CMAKE_CURRENT_BINARY_DIR}/../vm/wks)

if(FEATURE_STANDALONE_GC)
  add_library_clr(satorigc SHARED ${GC_SOURCES} $<TARGET_OBJECTS:satori_writebarrier>)
  add_dependencies(satorigc eventing_headers)
  target_link_libraries(satorigc PRIVATE ${GC_LINK_LIBRARIES})
  # target_link_libraries(satorigc PRIVATE gc_dll_wks_descriptor)
  # if(FEATURE_SVR_GC)
  #   target_link_libraries(satorigc PRIVATE gc_dll_svr_descriptor)
  # endif()
  install_clr(TARGETS satorigc DESTINATIONS . COMPONENT runtime)

  add_definitions(-DBUILD_AS_STANDALONE)

  add_definitions(-DFX_VER_INTERNALNAME_STR=satorigc.dll)
  add_definitions(-DVERIFY_HEAP)
  add_definitions(-DGC_CONFIG_DRIVEN)
  if(CLR_CMAKE_HOST_APPLE)
    # The implementation of GCToOSInterface on Apple platforms makes use of non-POSIX
    # pthreads APIs, which by default are not included in the pthreads header
    # unless we define this macro.
    add_definitions(-D_DARWIN_C_SOURCE)
  endif(CLR_CMAKE_HOST_APPLE)

  include_directories(../gc/env)

  # add_definitions(-DGC_DESCRIPTOR)
  # add_subdirectory(datadescriptor)

  install_clr(TARGETS satorigc DESTINATIONS . sharedFramework COMPONENT runtime)
endif(FEATURE_STANDALONE_GC)
