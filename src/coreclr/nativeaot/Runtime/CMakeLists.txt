set(GC_DIR ../../gc)

set(COMMON_RUNTIME_SOURCES
    allocheap.cpp
    rhassert.cpp
    CachedInterfaceDispatch.cpp
    Crst.cpp
    DebugHeader.cpp
    MethodTable.cpp
    EHHelpers.cpp
    event.cpp
    FinalizerHelpers.cpp
    GcEnum.cpp
    GCHelpers.cpp
    gctoclreventsink.cpp
    gcheaputilities.cpp
    GCMemoryHelpers.cpp
    gcenv.ee.cpp
    GcStressControl.cpp
    HandleTableHelpers.cpp
    MathHelpers.cpp
    MiscHelpers.cpp
    TypeManager.cpp
    ObjectLayout.cpp
    portable.cpp
    RestrictedCallouts.cpp
    RhConfig.cpp
    RuntimeInstance.cpp
    StackFrameIterator.cpp
    startup.cpp
    stressLog.cpp
    SyncClean.cpp
    thread.cpp
    threadstore.cpp
    UniversalTransitionHelpers.cpp
    yieldprocessornormalized.cpp

    ${GC_DIR}/gceventstatus.cpp
    ${GC_DIR}/gcload.cpp
    ${GC_DIR}/gcconfig.cpp
    ${GC_DIR}/gchandletable.cpp
    ${GC_DIR}/gccommon.cpp
    ${GC_DIR}/gceewks.cpp
    ${GC_DIR}/gcwks.cpp
    ${GC_DIR}/gcscan.cpp
    ${GC_DIR}/handletable.cpp
    ${GC_DIR}/handletablecache.cpp
    ${GC_DIR}/handletablecore.cpp
    ${GC_DIR}/handletablescan.cpp
    ${GC_DIR}/objecthandle.cpp
    ${GC_DIR}/softwarewritewatch.cpp

    ${CLR_SRC_NATIVE_DIR}/minipal/cpufeatures.c
)

set(SERVER_GC_SOURCES
    ${GC_DIR}/gceesvr.cpp
    ${GC_DIR}/gcsvr.cpp
)

set(STANDALONEGC_DISABLED_SOURCES
    clrgc.disabled.cpp
)

set(STANDALONEGC_ENABLED_SOURCES
    clrgc.enabled.cpp
)

set(FULL_RUNTIME_SOURCES
    AsmOffsetsVerify.cpp
    ThunksMapping.cpp
)

include_directories(inc)

include_directories(.)
include_directories(${GC_DIR})
include_directories(${GC_DIR}/env)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/eventpipe/inc)

if (WIN32)
  set(GC_HEADERS
    ${GC_DIR}/env/common.h
    ${GC_DIR}/env/etmdummy.h
    ${GC_DIR}/env/gcenv.base.h
    ${GC_DIR}/env/gcenv.ee.h
    ${GC_DIR}/env/gcenv.h
    ${GC_DIR}/env/gcenv.interlocked.h
    ${GC_DIR}/env/gcenv.interlocked.inl
    ${GC_DIR}/env/gcenv.object.h
    ${GC_DIR}/env/gcenv.os.h
    ${GC_DIR}/env/gcenv.structs.h
    ${GC_DIR}/env/gcenv.sync.h
    ${GC_DIR}/env/gcenv.windows.inl
    ${GC_DIR}/env/volatile.h
    ${GC_DIR}/gc.h
    ${GC_DIR}/gcconfig.h
    ${GC_DIR}/gcdesc.h
    ${GC_DIR}/gcenv.ee.standalone.inl
    ${GC_DIR}/gcenv.inl
    ${GC_DIR}/gcevent_serializers.h
    ${GC_DIR}/gcevents.h
    ${GC_DIR}/gceventstatus.h
    ${GC_DIR}/gchandletableimpl.h
    ${GC_DIR}/gcimpl.h
    ${GC_DIR}/gcinterface.dac.h
    ${GC_DIR}/gcinterface.ee.h
    ${GC_DIR}/gcinterface.h
    ${GC_DIR}/gcpriv.h
    ${GC_DIR}/gcrecord.h
    ${GC_DIR}/gcscan.h
    ${GC_DIR}/handletable.h
    ${GC_DIR}/handletable.inl
    ${GC_DIR}/handletablepriv.h
    ${GC_DIR}/objecthandle.h
    ${GC_DIR}/softwarewritewatch.h)

  include_directories(windows)

  list(APPEND COMMON_RUNTIME_SOURCES
    windows/PalRedhawkCommon.cpp
    windows/PalRedhawkMinWin.cpp
    ${GC_DIR}/windows/gcenv.windows.cpp
  )

  list(APPEND FULL_RUNTIME_SOURCES windows/CoffNativeCodeManager.cpp)

  set(ASM_SUFFIX asm)
else()

  include_directories(unix)

  # sal.h, pshpack/poppack.h
  add_definitions(-DPAL_STDCPP_COMPAT)
  include_directories(../../pal/inc/rt)

  include(CheckIncludeFiles)
  include(CheckLibraryExists)

  include(${GC_DIR}/unix/configure.cmake)

  if(CLR_CMAKE_TARGET_ARCH_WASM)
    include_directories($ENV{EMSCRIPTEN/system/lib/libcxxabi/include})
  endif()

  # Disable building _Unwind_XXX style APIs of libunwind, since we don't use them.
  add_definitions(-D_LIBUNWIND_DISABLE_ZERO_COST_APIS=1)

  # Compile unwinding only for the current compilation target architecture
  add_definitions(-D_LIBUNWIND_IS_NATIVE_ONLY)

  if(CLR_CMAKE_TARGET_HAIKU)
    add_definitions(-DPT_GNU_EH_FRAME=PT_EH_FRAME)
  endif()

  list(APPEND COMMON_RUNTIME_SOURCES
    unix/PalRedhawkUnix.cpp
    unix/PalCreateDump.cpp
    ${GC_DIR}/unix/gcenv.unix.cpp
    ${GC_DIR}/unix/numasupport.cpp
    ${GC_DIR}/unix/events.cpp
    ${GC_DIR}/unix/cgroup.cpp
  )

  list(APPEND FULL_RUNTIME_SOURCES
    unix/cgroupcpu.cpp
    unix/HardwareExceptions.cpp
    unix/UnixContext.cpp
    unix/UnixSignals.cpp
    unix/UnwindHelpers.cpp
    unix/UnixNativeCodeManager.cpp
  )

  include(${CLR_SRC_NATIVE_DIR}/external/llvm-libunwind.cmake)

  list(APPEND FULL_RUNTIME_SOURCES ${LLVM_LIBUNWIND_SOURCES})
  set(RUNTIME_SOURCES_ARCH_ASM ${LLVM_LIBUNWIND_ASM_SOURCES})

  set(ASM_SUFFIX S)
endif()

if (CLR_CMAKE_TARGET_APPLE)
  list(APPEND COMMON_RUNTIME_SOURCES
    interoplibinterface_objc.cpp
  )
endif (CLR_CMAKE_TARGET_APPLE)

if (CLR_CMAKE_TARGET_ARCH_AMD64 AND CLR_CMAKE_TARGET_WIN32)
  set(VXSORT_SOURCES
    ${GC_DIR}/vxsort/isa_detection.cpp
    ${GC_DIR}/vxsort/do_vxsort_avx2.cpp
    ${GC_DIR}/vxsort/do_vxsort_avx512.cpp
    ${GC_DIR}/vxsort/machine_traits.avx2.cpp
    ${GC_DIR}/vxsort/smallsort/bitonic_sort.AVX2.int64_t.generated.cpp
    ${GC_DIR}/vxsort/smallsort/bitonic_sort.AVX2.int32_t.generated.cpp
    ${GC_DIR}/vxsort/smallsort/bitonic_sort.AVX512.int64_t.generated.cpp
    ${GC_DIR}/vxsort/smallsort/bitonic_sort.AVX512.int32_t.generated.cpp
    ${GC_DIR}/vxsort/smallsort/avx2_load_mask_tables.cpp
  )

  set(DUMMY_VXSORT_SOURCES
    ${GC_DIR}/vxsort/dummy.cpp
  )
endif (CLR_CMAKE_TARGET_ARCH_AMD64 AND CLR_CMAKE_TARGET_WIN32)

if (NOT (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64))
  list(APPEND RUNTIME_SOURCES_ARCH_ASM
    ${ARCH_SOURCES_DIR}/Interlocked.${ASM_SUFFIX}
  )
endif (NOT (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64))

list(APPEND RUNTIME_SOURCES_ARCH_ASM
  ${ARCH_SOURCES_DIR}/AllocFast.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/ExceptionHandling.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/GcProbe.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/MiscStubs.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/PInvoke.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/InteropThunksHelpers.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/StubDispatch.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/UniversalTransition.${ASM_SUFFIX}
  ${ARCH_SOURCES_DIR}/WriteBarriers.${ASM_SUFFIX}
)

# Add architecture specific folder for looking up headers.
convert_to_absolute_path(ARCH_SOURCES_DIR ${ARCH_SOURCES_DIR})
include_directories(${ARCH_SOURCES_DIR})

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  set(FEATURE_PERFTRACING 1)
  set(FEATURE_EVENT_TRACE 1)
endif()

if(FEATURE_PERFTRACING)
  add_definitions(-DFEATURE_PERFTRACING)
endif()

if(FEATURE_EVENT_TRACE)
  add_definitions(-DFEATURE_EVENT_TRACE)
endif()

add_definitions(-DFEATURE_BASICFREEZE)
add_definitions(-DFEATURE_CONSERVATIVE_GC)
if(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64)
  add_definitions(-DFEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP)
  add_definitions(-DFEATURE_MANUALLY_MANAGED_CARD_BUNDLES)
endif(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64)
if(CLR_CMAKE_TARGET_ARCH_ARM)
  add_definitions(-DFEATURE_64BIT_ALIGNMENT)
endif(CLR_CMAKE_TARGET_ARCH_ARM)

add_definitions(-DFEATURE_CUSTOM_IMPORTS)
add_definitions(-DFEATURE_DYNAMIC_CODE)
add_compile_definitions($<$<CONFIG:Debug,Checked>:FEATURE_GC_STRESS>)
add_definitions(-DFEATURE_NATIVEAOT)
add_definitions(-DVERIFY_HEAP)
add_definitions(-DNATIVEAOT)
add_definitions(-DFEATURE_CACHED_INTERFACE_DISPATCH)
if(CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_ARM)
  add_definitions(-DINTERFACE_DISPATCH_CACHE_HAS_CELL_BACKPOINTER)
endif(CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_ARM)
add_definitions(-D_LIB)

# there is a problem with undefined symbols when this is set
# add_definitions(-DSTRESS_HEAP)

if(CLR_CMAKE_TARGET_WIN32)
  set(FEATURE_ETW 1)
  add_definitions(-DFEATURE_ETW)
  add_definitions(-DFEATURE_SUSPEND_REDIRECTION)
  if (CLR_CMAKE_TARGET_ARCH_AMD64)
    add_definitions(-DFEATURE_SPECIAL_USER_MODE_APC)
  endif()
else()
  if(NOT CLR_CMAKE_TARGET_MACCATALYST AND NOT CLR_CMAKE_TARGET_IOS AND NOT CLR_CMAKE_TARGET_TVOS)
    add_definitions(-DFEATURE_READONLY_GS_COOKIE)
  endif()
  include(unix/configure.cmake)
  include(${CLR_SRC_NATIVE_DIR}/minipal/configure.cmake)
  include_directories(${CMAKE_CURRENT_BINARY_DIR})
endif()

if (CLR_CMAKE_TARGET_UNIX)

  if (CLR_CMAKE_TARGET_ARCH_AMD64)
    add_definitions(-DUNIX_AMD64_ABI)
  elseif (CLR_CMAKE_TARGET_ARCH_ARM)
    add_definitions(-DUNIX_ARM_ABI)
  elseif (CLR_CMAKE_TARGET_ARCH_I386)
    add_definitions(-DUNIX_X86_ABI)
  endif()

endif(CLR_CMAKE_TARGET_UNIX)

# use standards compliant preprocessor for MSVC, necessary for __VA_OPT__ support
if (MSVC)
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zc:preprocessor>)
endif()

set(RUNTIME_DIR ${CMAKE_CURRENT_SOURCE_DIR})

list(APPEND COMMON_RUNTIME_SOURCES ${GC_HEADERS})

convert_to_absolute_path(COMMON_RUNTIME_SOURCES ${COMMON_RUNTIME_SOURCES})
convert_to_absolute_path(FULL_RUNTIME_SOURCES ${FULL_RUNTIME_SOURCES})
convert_to_absolute_path(SERVER_GC_SOURCES ${SERVER_GC_SOURCES})
convert_to_absolute_path(STANDALONEGC_DISABLED_SOURCES ${STANDALONEGC_DISABLED_SOURCES})
convert_to_absolute_path(STANDALONEGC_ENABLED_SOURCES ${STANDALONEGC_ENABLED_SOURCES})
convert_to_absolute_path(RUNTIME_SOURCES_ARCH_ASM ${RUNTIME_SOURCES_ARCH_ASM})
convert_to_absolute_path(VXSORT_SOURCES ${VXSORT_SOURCES})
convert_to_absolute_path(DUMMY_VXSORT_SOURCES ${DUMMY_VXSORT_SOURCES})

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  add_subdirectory(Full)
else()
  add_subdirectory(Portable)
endif()

if(FEATURE_PERFTRACING)
  add_subdirectory(eventpipe)
endif()
