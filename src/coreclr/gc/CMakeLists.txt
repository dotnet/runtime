set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(FEATURE_STANDALONE_GC)
  add_definitions(-DFEATURE_STANDALONE_GC)
  remove_definitions(-DSTRESS_HEAP)
  remove_definitions(-DWRITE_BARRIER_CHECK)
endif(FEATURE_STANDALONE_GC)

set(GC_SOURCES
  gceventstatus.cpp
  gcconfig.cpp
  gccommon.cpp
  gcscan.cpp
  handletable.cpp
  handletablecore.cpp
  handletablescan.cpp
  objecthandle.cpp
  softwarewritewatch.cpp
  gchandletable.cpp
  gcload.cpp
  gcbridge.cpp
  handletablecache.cpp)

if(CLR_CMAKE_TARGET_ARCH_WASM)
  add_subdirectory(wasm)
elseif(CLR_CMAKE_HOST_UNIX)
  add_subdirectory(unix)
  include(unix/configure.cmake)
else()
  add_subdirectory(windows)
  set (GC_SOURCES
    ${GC_SOURCES}
    windows/Native.rc)
endif()

if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  add_subdirectory(vxsort)
endif (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)

if(CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  set(GC_WRITE_BARRIER_SOURCES
    copywritebarriermanager.cpp)
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
  set(GC_WRITE_BARRIER_SOURCES
    arm/writebarriermanager.cpp)
elseif(CLR_CMAKE_TARGET_ARCH_I386)
  set(GC_WRITE_BARRIER_SOURCES
    i386/writebarriermanager.cpp)
elseif(CLR_CMAKE_TARGET_ARCH_LOONGARCH64 OR CLR_CMAKE_TARGET_ARCH_RISCV64)
  set(GC_WRITE_BARRIER_SOURCES
    literalwritebarriermanager.cpp)
elseif(CLR_CMAKE_TARGET_ARCH_WASM)
  set(GC_WRITE_BARRIER_SOURCES
    staticwritebarriermanager.cpp
    wasm/WriteBarriers.cpp)
endif()

list(APPEND GC_WRITE_BARRIER_SOURCES
  writebarrierstomp.cpp)

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  list(APPEND GC_WRITE_BARRIER_SOURCES
    writebarrierservices.cpp)
endif()

set(GC_WRITE_BARRIER_COMMON_SOURCES ${GC_WRITE_BARRIER_SOURCES})

if(CLR_CMAKE_TARGET_ARCH_AMD64)
  if(CLR_CMAKE_TARGET_WIN32)
    set(GC_WRITE_BARRIER_ASM_SOURCES
      ${CMAKE_CURRENT_SOURCE_DIR}/amd64/JitHelpers_FastWriteBarriers.asm
      ${CMAKE_CURRENT_SOURCE_DIR}/amd64/JitHelpers_Slow.asm
      ${CMAKE_CURRENT_SOURCE_DIR}/amd64/patchedcode.asm
      ${CMAKE_CURRENT_SOURCE_DIR}/amd64/WriteBarriers.asm)

    if(CLR_CMAKE_HOST_ARCH_ARM OR CLR_CMAKE_HOST_ARCH_ARM64)
      set(GC_WRITE_BARRIER_ASM_INPUTS ${GC_WRITE_BARRIER_ASM_SOURCES})
      get_directory_property(GC_WRITE_BARRIER_INCLUDE_DIRECTORIES INCLUDE_DIRECTORIES)
      get_directory_property(GC_WRITE_BARRIER_COMPILE_DEFINITIONS COMPILE_DEFINITIONS)
      include_directories(../vm/${ARCH_SOURCES_DIR})
      if(CMAKE_BUILD_TYPE STREQUAL "Debug" OR CMAKE_BUILD_TYPE STREQUAL "Checked")
        add_definitions(-DWRITE_BARRIER_CHECK)
      endif()
      preprocess_files(GC_WRITE_BARRIER_ASM_SOURCES ${GC_WRITE_BARRIER_ASM_SOURCES})
      set_property(DIRECTORY PROPERTY INCLUDE_DIRECTORIES "${GC_WRITE_BARRIER_INCLUDE_DIRECTORIES}")
      set_property(DIRECTORY PROPERTY COMPILE_DEFINITIONS "${GC_WRITE_BARRIER_COMPILE_DEFINITIONS}")
    endif()
  else()
    set(GC_WRITE_BARRIER_ASM_SOURCES
      amd64/jithelpers_fastwritebarriers.S
      amd64/jithelpers_slow.S
      amd64/patchedcode.S
      amd64/WriteBarriers.S)
  endif()

  list(APPEND GC_WRITE_BARRIER_SOURCES
    ${GC_WRITE_BARRIER_ASM_SOURCES})
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
  if(CLR_CMAKE_TARGET_WIN32)
    set(GC_WRITE_BARRIER_ASM_SOURCES
      ${CMAKE_CURRENT_SOURCE_DIR}/arm64/patchedcode.asm
      ${CMAKE_CURRENT_SOURCE_DIR}/arm64/WriteBarriers.asm)

    if(CLR_CMAKE_HOST_ARCH_ARM OR CLR_CMAKE_HOST_ARCH_ARM64)
      set(GC_WRITE_BARRIER_ASM_INPUTS ${GC_WRITE_BARRIER_ASM_SOURCES})
      get_directory_property(GC_WRITE_BARRIER_INCLUDE_DIRECTORIES INCLUDE_DIRECTORIES)
      get_directory_property(GC_WRITE_BARRIER_COMPILE_DEFINITIONS COMPILE_DEFINITIONS)
      include_directories(../vm/${ARCH_SOURCES_DIR})
      if(CMAKE_BUILD_TYPE STREQUAL "Debug" OR CMAKE_BUILD_TYPE STREQUAL "Checked")
        add_definitions(-DWRITE_BARRIER_CHECK)
      endif()
      preprocess_files(GC_WRITE_BARRIER_ASM_SOURCES ${GC_WRITE_BARRIER_ASM_SOURCES})
      set_property(DIRECTORY PROPERTY INCLUDE_DIRECTORIES "${GC_WRITE_BARRIER_INCLUDE_DIRECTORIES}")
      set_property(DIRECTORY PROPERTY COMPILE_DEFINITIONS "${GC_WRITE_BARRIER_COMPILE_DEFINITIONS}")
    endif()
  else()
    set(GC_WRITE_BARRIER_ASM_SOURCES
      arm64/patchedcode.S
      arm64/WriteBarriers.S)
  endif()

  list(APPEND GC_WRITE_BARRIER_SOURCES
    ${GC_WRITE_BARRIER_ASM_SOURCES})
elseif(CLR_CMAKE_TARGET_ARCH_I386)
  if(CLR_CMAKE_TARGET_WIN32)
    set(GC_WRITE_BARRIER_ASM_SOURCES
      i386/checkedwritebarriers.asm
      i386/WriteBarriers.asm)
  else()
    set(GC_WRITE_BARRIER_ASM_SOURCES
      i386/checkedwritebarriers.S
      i386/WriteBarriers.S)
  endif()

  list(APPEND GC_WRITE_BARRIER_SOURCES
    ${GC_WRITE_BARRIER_ASM_SOURCES})
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
  set(GC_WRITE_BARRIER_ASM_SOURCES
    arm/patchedcode.S
    arm/WriteBarriers.S)

  list(APPEND GC_WRITE_BARRIER_SOURCES
    ${GC_WRITE_BARRIER_ASM_SOURCES})
elseif(CLR_CMAKE_TARGET_ARCH_LOONGARCH64)
  set(GC_WRITE_BARRIER_ASM_SOURCES
    loongarch64/WriteBarriers.S)

  list(APPEND GC_WRITE_BARRIER_SOURCES
    ${GC_WRITE_BARRIER_ASM_SOURCES})
elseif(CLR_CMAKE_TARGET_ARCH_RISCV64)
  set(GC_WRITE_BARRIER_ASM_SOURCES
    riscv64/WriteBarriers.S)

  list(APPEND GC_WRITE_BARRIER_SOURCES
    ${GC_WRITE_BARRIER_ASM_SOURCES})
endif()

add_library_clr(gc_writebarrier OBJECT ${GC_WRITE_BARRIER_SOURCES})
target_include_directories(gc_writebarrier PRIVATE
  ${ARCH_SOURCES_DIR}
  ../pal/inc
  ../vm/${ARCH_SOURCES_DIR}
  ${CMAKE_CURRENT_BINARY_DIR}/../vm/wks)

foreach (Config DEBUG CHECKED)
  target_compile_definitions(gc_writebarrier PRIVATE $<$<CONFIG:${Config}>:WRITE_BARRIER_CHECK>)
endforeach (Config)

if(FEATURE_STANDALONE_GC)
  set(CLRGC_WRITE_BARRIER_ASM_SOURCES ${GC_WRITE_BARRIER_ASM_SOURCES})
  if(GC_WRITE_BARRIER_ASM_INPUTS)
    get_directory_property(GC_WRITE_BARRIER_INCLUDE_DIRECTORIES INCLUDE_DIRECTORIES)
    get_directory_property(GC_WRITE_BARRIER_COMPILE_DEFINITIONS COMPILE_DEFINITIONS)
    include_directories(../vm/${ARCH_SOURCES_DIR})
    remove_definitions(-DWRITE_BARRIER_CHECK)
    add_definitions(-DGC_WRITE_BARRIER_STANDALONE)
    set(CLRGC_WRITE_BARRIER_ASM_SOURCES)
    foreach(ASM_FILE IN LISTS GC_WRITE_BARRIER_ASM_INPUTS)
      get_filename_component(ASM_NAME ${ASM_FILE} NAME_WE)
      set(ASM_PREPROCESSED_FILE
        ${CMAKE_CURRENT_BINARY_DIR}/standalone_${ASM_NAME}.asm)
      preprocess_file(${ASM_FILE} ${ASM_PREPROCESSED_FILE})
      list(APPEND CLRGC_WRITE_BARRIER_ASM_SOURCES ${ASM_PREPROCESSED_FILE})
    endforeach()
    set_property(DIRECTORY PROPERTY INCLUDE_DIRECTORIES "${GC_WRITE_BARRIER_INCLUDE_DIRECTORIES}")
    set_property(DIRECTORY PROPERTY COMPILE_DEFINITIONS "${GC_WRITE_BARRIER_COMPILE_DEFINITIONS}")
  endif()

  set(CLRGC_WRITE_BARRIER_SOURCES
    ${GC_WRITE_BARRIER_COMMON_SOURCES}
    ${CLRGC_WRITE_BARRIER_ASM_SOURCES})
  add_library_clr(clrgc_writebarrier OBJECT ${CLRGC_WRITE_BARRIER_SOURCES})
  target_include_directories(clrgc_writebarrier PRIVATE
    ${ARCH_SOURCES_DIR}
    ../pal/inc
    ../vm/${ARCH_SOURCES_DIR}
    ${CMAKE_CURRENT_BINARY_DIR}/../vm/wks)
  target_compile_definitions(clrgc_writebarrier PRIVATE
    BUILD_AS_STANDALONE
    FEATURE_CONSERVATIVE_GC
    GC_WRITE_BARRIER_STANDALONE)
endif(FEATURE_STANDALONE_GC)

if (CLR_CMAKE_TARGET_WIN32)
  set(GC_HEADERS
    env/common.h
    env/etmdummy.h
    env/gcenv.base.h
    env/gcenv.ee.h
    env/gcenv.h
    env/gcenv.interlocked.h
    env/gcenv.interlocked.inl
    env/gcenv.object.h
    env/gcenv.os.h
    env/gcenv.structs.h
    env/gcenv.sync.h
    env/gcenv.windows.inl
    env/volatile.h
    gc.h
    gcinternal.h
    gcconfig.h
    gcbridge.h
    gcdesc.h
    gcenv.ee.standalone.inl
    gcenv.inl
    gcevent_serializers.h
    gcevents.h
    gceventstatus.h
    gchandletableimpl.h
    gcimpl.h
    gcinterface.dac.h
    gcinterface.ee.h
    gcinterface.h
    gcpriv.h
    gcrecord.h
    gcscan.h
    handletable.h
    handletable.inl
    handletablepriv.h
    objecthandle.h
    softwarewritewatch.h
    writebarrier.h
    writebarrierglobals.h
  )
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_HOST_WIN32)
  set (GC_LINK_LIBRARIES
    ${STATIC_MT_CRT_LIB}
    ${STATIC_MT_VCRT_LIB}
    kernel32.lib
    advapi32.lib)
endif(CLR_CMAKE_HOST_WIN32)

set (GC_LINK_LIBRARIES
    ${GC_LINK_LIBRARIES}
    gc_pal
    minipal)

if(CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
    list(APPEND GC_LINK_LIBRARIES
        gc_vxsort
    )
endif(CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)


list(APPEND GC_SOURCES ${GC_HEADERS})

convert_to_absolute_path(GC_SOURCES ${GC_SOURCES})

set(GC_WKS_SVR_SOURCES
  bulkwritebarrier.cpp
  gcee.cpp
  gc.cpp
  init.cpp
  no_gc.cpp
  finalization.cpp
  dynamic_tuning.cpp
  region_free_list.cpp
  region_allocator.cpp
  memory.cpp
  sweep.cpp
  collect.cpp
  diagnostics.cpp
  dynamic_heap_count.cpp
  card_table.cpp
  relocate_compact.cpp
  mark_phase.cpp
  background.cpp
  interface.cpp
  allocation.cpp
  plan_phase.cpp
  regions_segments.cpp)

if(FEATURE_STANDALONE_GC)
  if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
    set(BUILD_EXP_GC 1)
  endif()

  # clrgcexp is build with standalone+regions
  if (BUILD_EXP_GC)
    add_library_clr(clrgcexp_gc_wks OBJECT ${GC_WKS_SVR_SOURCES})
    set_target_properties(clrgcexp_gc_wks PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)
    add_dependencies(clrgcexp_gc_wks eventing_headers)
    target_compile_definitions(clrgcexp_gc_wks PRIVATE USE_REGIONS)
    set(CLRGGCEXP_OBJECTS
      $<TARGET_OBJECTS:clrgcexp_gc_wks>)
    if (FEATURE_SVR_GC)
      add_library_clr(clrgcexp_gc_svr OBJECT ${GC_WKS_SVR_SOURCES})
      set_target_properties(clrgcexp_gc_svr PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)
      add_dependencies(clrgcexp_gc_svr eventing_headers)
      target_compile_definitions(clrgcexp_gc_svr PRIVATE SERVER_GC USE_REGIONS)
      list(APPEND CLRGGCEXP_OBJECTS
        $<TARGET_OBJECTS:clrgcexp_gc_svr>)
    endif()

    add_library_clr(clrgcexp SHARED
      ${GC_SOURCES}
      ${CLRGGCEXP_OBJECTS}
      $<TARGET_OBJECTS:clrgc_writebarrier>)
    add_dependencies(clrgcexp eventing_headers)
    target_link_libraries(clrgcexp PRIVATE ${GC_LINK_LIBRARIES})
    target_link_libraries(clrgcexp PRIVATE gcexp_dll_wks_descriptor)
    if (FEATURE_SVR_GC)
      target_link_libraries(clrgcexp PRIVATE gcexp_dll_svr_descriptor)
    endif()
    target_compile_definitions(clrgcexp PRIVATE -DUSE_REGIONS)
    install_clr(TARGETS clrgcexp DESTINATIONS . COMPONENT runtime)
  endif (BUILD_EXP_GC)

  add_library_clr(clrgc_gc_wks OBJECT ${GC_WKS_SVR_SOURCES})
  add_dependencies(clrgc_gc_wks eventing_headers)
  set(CLRGC_OBJECTS
    $<TARGET_OBJECTS:clrgc_gc_wks>)
  if(FEATURE_SVR_GC)
    add_library_clr(clrgc_gc_svr OBJECT ${GC_WKS_SVR_SOURCES})
    add_dependencies(clrgc_gc_svr eventing_headers)
    target_compile_definitions(clrgc_gc_svr PRIVATE SERVER_GC)
    list(APPEND CLRGC_OBJECTS
      $<TARGET_OBJECTS:clrgc_gc_svr>)
  endif()

  # clrgc is build with standalone+segments
  add_library_clr(clrgc SHARED
    ${GC_SOURCES}
    ${CLRGC_OBJECTS}
    $<TARGET_OBJECTS:clrgc_writebarrier>)
  add_dependencies(clrgc eventing_headers)
  target_link_libraries(clrgc PRIVATE ${GC_LINK_LIBRARIES})
  target_link_libraries(clrgc PRIVATE gc_dll_wks_descriptor)
  if(FEATURE_SVR_GC)
    target_link_libraries(clrgc PRIVATE gc_dll_svr_descriptor)
  endif()
  install_clr(TARGETS clrgc DESTINATIONS . COMPONENT runtime)

  add_definitions(-DBUILD_AS_STANDALONE)
  add_definitions(-DFEATURE_CONSERVATIVE_GC)

  add_definitions(-DFX_VER_INTERNALNAME_STR=clrgc.dll)
  add_definitions(-DVERIFY_HEAP=)
  add_definitions(-DGC_CONFIG_DRIVEN=)
  if(CLR_CMAKE_HOST_APPLE)
    # The implementation of GCToOSInterface on Apple platforms makes use of non-POSIX
    # pthreads APIs, which by default are not included in the pthreads header
    # unless we define this macro.
    add_definitions(-D_DARWIN_C_SOURCE)
  endif(CLR_CMAKE_HOST_APPLE)

  include_directories(${CMAKE_CURRENT_SOURCE_DIR})
  include_directories(${CMAKE_CURRENT_SOURCE_DIR}/env)

  add_definitions(-DGC_DESCRIPTOR)
  add_subdirectory(datadescriptor)

  install_clr(TARGETS clrgc DESTINATIONS . sharedFramework COMPONENT runtime)
  if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
    install_clr(TARGETS clrgcexp DESTINATIONS . sharedFramework COMPONENT runtime)
  endif (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
endif(FEATURE_STANDALONE_GC)
