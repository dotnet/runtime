project(Runtime)

set(FEATURE_PORTABLE_HELPERS 0)
if(CLR_CMAKE_TARGET_ARCH_WASM)
  set(FEATURE_PORTABLE_HELPERS 1)
endif()

set(GC_DIR ../../gc)

set(GC_WKS_SVR_SOURCES
    ${GC_DIR}/gc.cpp
    ${GC_DIR}/init.cpp
    ${GC_DIR}/no_gc.cpp
    ${GC_DIR}/finalization.cpp
    ${GC_DIR}/dynamic_tuning.cpp
    ${GC_DIR}/region_free_list.cpp
    ${GC_DIR}/region_allocator.cpp
    ${GC_DIR}/memory.cpp
    ${GC_DIR}/sweep.cpp
    ${GC_DIR}/collect.cpp
    ${GC_DIR}/diagnostics.cpp
    ${GC_DIR}/dynamic_heap_count.cpp
    ${GC_DIR}/card_table.cpp
    ${GC_DIR}/relocate_compact.cpp
    ${GC_DIR}/mark_phase.cpp
    ${GC_DIR}/background.cpp
    ${GC_DIR}/interface.cpp
    ${GC_DIR}/allocation.cpp
    ${GC_DIR}/plan_phase.cpp
    ${GC_DIR}/regions_segments.cpp
    ${GC_DIR}/gcee.cpp
)

set(COMMON_RUNTIME_SOURCES
    AsmOffsetsVerify.cpp
    rhassert.cpp
    ${RUNTIME_DIR}/MathHelpers.cpp
    ${RUNTIME_DIR}/MiscNativeHelpers.cpp
    Crst.cpp
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
    HandleTableHelpers.cpp
    interoplibinterface_java.cpp
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
    ${GC_DIR}/gcbridge.cpp
    ${GC_DIR}/gcload.cpp
    ${GC_DIR}/gcconfig.cpp
    ${GC_DIR}/gchandletable.cpp
    ${GC_DIR}/gccommon.cpp
    ${GC_DIR}/gcscan.cpp
    ${GC_DIR}/handletable.cpp
    ${GC_DIR}/handletablecache.cpp
    ${GC_DIR}/handletablecore.cpp
    ${GC_DIR}/handletablescan.cpp
    ${GC_DIR}/objecthandle.cpp
    ${GC_DIR}/softwarewritewatch.cpp
    ${CLR_SRC_NATIVE_DIR}/minipal/xoshiro128pp.c
)

if (CLR_CMAKE_TARGET_UNIX AND NOT CLR_CMAKE_TARGET_ARCH_WASM)
    list(APPEND COMMON_RUNTIME_SOURCES
        ${RUNTIME_DIR}/SignalSafeThreadMap.cpp
    )
endif()

set(STANDALONEGC_DISABLED_SOURCES
    clrgc.disabled.cpp
)

set(STANDALONEGC_ENABLED_SOURCES
    clrgc.enabled.cpp
)

if(FEATURE_PORTABLE_HELPERS)
  list(APPEND COMMON_RUNTIME_SOURCES ${RUNTIME_DIR}/portable/AllocFast.cpp)
else()
  list(APPEND COMMON_RUNTIME_SOURCES ThunksMapping.cpp)
endif()

include_directories(inc)

include_directories(.)
include_directories(${GC_DIR})
include_directories(${GC_DIR}/env)
include_directories(${CMAKE_CURRENT_BINARY_DIR}/eventpipe/inc)
include_directories(${RUNTIME_DIR})

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
    ${RUNTIME_DIR}/windows/CLREventBase.cpp
    windows/PalCommon.cpp
    windows/PalMinWin.cpp
    ${GC_DIR}/windows/gcenv.windows.cpp
  )

  if(NOT FEATURE_PORTABLE_HELPERS)
    list(APPEND COMMON_RUNTIME_SOURCES windows/CoffNativeCodeManager.cpp)

    if(CLR_CMAKE_TARGET_ARCH_ARM64)
      list(APPEND COMMON_RUNTIME_SOURCES
        ../../unwinder/arm64/unwinder.cpp
      )
      include_directories(../../inc)
      include_directories(../../unwinder)
      include_directories(../../unwinder/arm64)
    endif()
  endif()

  set(ASM_SUFFIX asm)
else()

  include_directories(unix)

  # sal.h, pshpack/poppack.h
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

  include(CheckSymbolExists)
  check_symbol_exists(getauxval "sys/auxv.h" HAVE_GETAUXVAL)
  if (HAVE_GETAUXVAL)
    add_definitions(-D_LIBUNWIND_HAVE_GETAUXVAL)
  endif()

  check_symbol_exists(elf_aux_info "sys/auxv.h" HAVE_ELF_AUX_INFO)
  if (HAVE_ELF_AUX_INFO)
    add_definitions(-D_LIBUNWIND_HAVE_ELF_AUX_INFO)
  endif()

  if(CLR_CMAKE_TARGET_HAIKU)
    add_definitions(-DPT_GNU_EH_FRAME=PT_EH_FRAME)
  endif()

  list(APPEND COMMON_RUNTIME_SOURCES
    ${RUNTIME_DIR}/unix/CLREventBase.cpp
    unix/PalUnix.cpp
    unix/PalCreateDump.cpp
    unix/cgroupcpu.cpp
    ${GC_DIR}/unix/events.cpp
  )

  if(CLR_CMAKE_TARGET_ARCH_WASM)
    list(APPEND COMMON_RUNTIME_SOURCES
      wasm/ExceptionHandling.cpp
      wasm/MiscStubs.cpp
      wasm/PInvoke.cpp
      wasm/WasmCodeManager.cpp
      ${RUNTIME_DIR}/wasm/writebarriers.cpp
      ${GC_DIR}/wasm/gcenv.cpp
    )
    include_directories(${GC_DIR}/unix)
  else()
    list(APPEND COMMON_RUNTIME_SOURCES
      ${GC_DIR}/unix/gcenv.unix.cpp
      ${GC_DIR}/unix/numasupport.cpp
      ${GC_DIR}/unix/cgroup.cpp
    )
  endif()

  if(NOT FEATURE_PORTABLE_HELPERS)
    list(APPEND COMMON_RUNTIME_SOURCES
      unix/HardwareExceptions.cpp
      unix/NativeContext.cpp
      unix/UnixSignals.cpp
      unix/UnwindHelpers.cpp
      unix/UnixNativeCodeManager.cpp
    )

    include(${CLR_SRC_NATIVE_DIR}/external/llvm-libunwind.cmake)

    set(NATIVEAOT_LOCALIZE_LIBUNWIND_SYMBOLS OFF)
    if(CLR_CMAKE_TARGET_UNIX AND NOT CLR_CMAKE_TARGET_APPLE AND NOT CLR_CMAKE_TARGET_ARCH_WASM)
      set(NATIVEAOT_LOCALIZE_LIBUNWIND_SYMBOLS ON)
    endif()

    if(NATIVEAOT_LOCALIZE_LIBUNWIND_SYMBOLS)
      # Build the bundled libunwind implementation and the runtime unwinder code
      # that references it into one relocatable object. The runtime sources are
      # part of the partial link so their references to libunwind's implementation
      # symbols are resolved before those symbols are localized.
      set(NATIVEAOT_PRIVATE_LIBUNWIND_SOURCES
        unix/UnwindHelpers.cpp
        unix/UnixNativeCodeManager.cpp
        ${LLVM_LIBUNWIND_SOURCES}
        ${LLVM_LIBUNWIND_ASM_SOURCES})
      list(REMOVE_ITEM COMMON_RUNTIME_SOURCES unix/UnwindHelpers.cpp unix/UnixNativeCodeManager.cpp)
    else()
      list(APPEND COMMON_RUNTIME_SOURCES ${LLVM_LIBUNWIND_SOURCES})
      set(RUNTIME_SOURCES_ARCH_ASM ${LLVM_LIBUNWIND_ASM_SOURCES})
    endif()
  endif()

  set(ASM_SUFFIX S)
endif()

if (CLR_CMAKE_TARGET_APPLE)
  list(APPEND COMMON_RUNTIME_SOURCES
    interoplibinterface_objc.cpp
  )
endif (CLR_CMAKE_TARGET_APPLE)

if (CLR_CMAKE_TARGET_ARCH_AMD64)
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
endif (CLR_CMAKE_TARGET_ARCH_AMD64)

if (CLR_CMAKE_TARGET_ARCH_ARM64)
set (VXSORT_SOURCES
    ${GC_DIR}/vxsort/isa_detection.cpp
    ${GC_DIR}/vxsort/do_vxsort_neon.cpp
    ${GC_DIR}/vxsort/machine_traits.neon.cpp
    ${GC_DIR}/vxsort/smallsort/bitonic_sort.NEON.uint32_t.generated.cpp
    ${GC_DIR}/vxsort/smallsort/bitonic_sort.scalar.uint64_t.generated.cpp
)
endif (CLR_CMAKE_TARGET_ARCH_ARM64)

if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  set(DUMMY_VXSORT_SOURCES
    ${GC_DIR}/vxsort/dummy.cpp
  )
endif (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)

if(NOT FEATURE_PORTABLE_HELPERS)
  list(APPEND RUNTIME_SOURCES_ARCH_ASM
    ${RUNTIME_DIR}/${ARCH_SOURCES_DIR}/AllocFast.${ASM_SUFFIX}
    ${ARCH_SOURCES_DIR}/DispatchResolve.${ASM_SUFFIX}
    ${ARCH_SOURCES_DIR}/ExceptionHandling.${ASM_SUFFIX}
    ${ARCH_SOURCES_DIR}/GcProbe.${ASM_SUFFIX}
    ${RUNTIME_DIR}/${ARCH_SOURCES_DIR}/MiscStubs.${ASM_SUFFIX}
    ${ARCH_SOURCES_DIR}/PInvoke.${ASM_SUFFIX}
    ${ARCH_SOURCES_DIR}/UniversalTransition.${ASM_SUFFIX}
    ${RUNTIME_DIR}/${ARCH_SOURCES_DIR}/WriteBarriers.${ASM_SUFFIX}
  )

  if(CLR_CMAKE_TARGET_APPLE)
    list(APPEND RUNTIME_SOURCES_ARCH_ASM
      ${ARCH_SOURCES_DIR}/ThunkPoolThunks.${ASM_SUFFIX}
    )
  endif()
endif()

if (NOT CLR_CMAKE_TARGET_ARCH_WASM)
  list(APPEND RUNTIME_SOURCES_ARCH_ASM
    ${RUNTIME_DIR}/${ARCH_SOURCES_DIR}/EHHelpers.${ASM_SUFFIX}
  )
endif (NOT CLR_CMAKE_TARGET_ARCH_WASM)

# Add architecture specific folder for looking up headers.
convert_to_absolute_path(ARCH_SOURCES_DIR ${ARCH_SOURCES_DIR})
include_directories(${ARCH_SOURCES_DIR})

set(FEATURE_PERFTRACING 1)
set(FEATURE_EVENT_TRACE 1)

if(FEATURE_PERFTRACING)
  add_definitions(-DFEATURE_PERFTRACING)
endif()

if(FEATURE_EVENT_TRACE)
  add_definitions(-DFEATURE_EVENT_TRACE)
endif()

if (NOT CLR_CMAKE_TARGET_ARCH_WASM)
  add_definitions(-DFEATURE_HIJACK)
endif()

add_definitions(-DFEATURE_CONSERVATIVE_GC)
if(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_RISCV64 OR CLR_CMAKE_TARGET_ARCH_LOONGARCH64)
  add_definitions(-DFEATURE_USE_SOFTWARE_WRITE_WATCH_FOR_GC_HEAP)
  add_definitions(-DFEATURE_MANUALLY_MANAGED_CARD_BUNDLES)
endif()
if(CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_WASM)
  add_definitions(-DFEATURE_64BIT_ALIGNMENT)
endif()

add_definitions(-DFEATURE_NATIVEAOT)
add_definitions(-DVERIFY_HEAP)
add_definitions(-DNATIVEAOT)
add_definitions(-D_LIB)
if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  add_definitions(-DGC_DESCRIPTOR)
endif()

# there is a problem with undefined symbols when this is set
# add_definitions(-DSTRESS_HEAP)

if(CLR_CMAKE_TARGET_WIN32)
  set(FEATURE_ETW 1)
  add_definitions(-DFEATURE_ETW)
  add_definitions(-DFEATURE_SUSPEND_REDIRECTION)
  if (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64)
    add_definitions(-DFEATURE_SPECIAL_USER_MODE_APC)
  endif()
  if (CLR_CMAKE_TARGET_ARCH_I386)
    add_compile_options($<$<COMPILE_LANGUAGE:ASM_MASM>:/safeseh>)
  endif()
else()
  # OpenBSD marks the main executable's read-only segments immutable (mimmutable) at load,
  # so the runtime cannot mprotect the read-only GS cookie page to writable to initialize it.
  # Keep the cookie in writable memory there, like Apple.
  if(NOT CLR_CMAKE_TARGET_APPLE AND NOT CLR_CMAKE_TARGET_ARCH_WASM AND NOT CLR_CMAKE_TARGET_OPENBSD)
    add_definitions(-DFEATURE_READONLY_GS_COOKIE)
  endif()
  include(unix/configure.cmake)
  include_directories(${CMAKE_CURRENT_BINARY_DIR})
endif()

if (CLR_CMAKE_TARGET_UNIX)

  if (CLR_CMAKE_TARGET_ARCH_AMD64)
    add_definitions(-DUNIX_AMD64_ABI)
  elseif (CLR_CMAKE_TARGET_ARCH_ARM)
    add_definitions(-DUNIX_ARM_ABI)
  endif()

endif(CLR_CMAKE_TARGET_UNIX)

set(NATIVEAOT_RUNTIME_DIR ${CMAKE_CURRENT_SOURCE_DIR})

list(APPEND COMMON_RUNTIME_SOURCES ${GC_HEADERS})

convert_to_absolute_path(COMMON_RUNTIME_SOURCES ${COMMON_RUNTIME_SOURCES})
convert_to_absolute_path(GC_WKS_SVR_SOURCES ${GC_WKS_SVR_SOURCES})
convert_to_absolute_path(STANDALONEGC_DISABLED_SOURCES ${STANDALONEGC_DISABLED_SOURCES})
convert_to_absolute_path(STANDALONEGC_ENABLED_SOURCES ${STANDALONEGC_ENABLED_SOURCES})
convert_to_absolute_path(NATIVEAOT_PRIVATE_LIBUNWIND_SOURCES ${NATIVEAOT_PRIVATE_LIBUNWIND_SOURCES})
convert_to_absolute_path(RUNTIME_SOURCES_ARCH_ASM ${RUNTIME_SOURCES_ARCH_ASM})
convert_to_absolute_path(VXSORT_SOURCES ${VXSORT_SOURCES})
convert_to_absolute_path(DUMMY_VXSORT_SOURCES ${DUMMY_VXSORT_SOURCES})

if(FEATURE_PERFTRACING)
  add_subdirectory(eventpipe)
endif()

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  # Create an interface library that captures the Runtime's include directories
  # for use by the datadescriptor intermediary compilation.
  add_library(nativeaot_runtime_includes INTERFACE)
  get_property(_runtime_includes DIRECTORY ${CMAKE_CURRENT_SOURCE_DIR} PROPERTY INCLUDE_DIRECTORIES)
  target_include_directories(nativeaot_runtime_includes INTERFACE ${_runtime_includes})

  add_subdirectory(datadescriptor)
endif()

if(FEATURE_PORTABLE_HELPERS)
  add_definitions(-DFEATURE_PORTABLE_HELPERS)
else()
  set(CMAKE_INCLUDE_CURRENT_DIR ON)

  if(NOT CLR_CMAKE_TARGET_APPLE)
    add_definitions(-DFEATURE_RX_THUNKS)
  endif()

  if(CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_HOST_ARCH_ARM64)
    # Needed to include AsmOffsets.inc
    include_directories(BEFORE ${CMAKE_CURRENT_BINARY_DIR})
    preprocess_files(RUNTIME_SOURCES_ARCH_ASM ${RUNTIME_SOURCES_ARCH_ASM})
  endif()
endif()

if(NATIVEAOT_PRIVATE_LIBUNWIND_SOURCES)
  add_library(Runtime.PrivateLibunwind OBJECT ${NATIVEAOT_PRIVATE_LIBUNWIND_SOURCES})

  set(NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT "${CMAKE_CURRENT_BINARY_DIR}/Runtime.PrivateLibunwind.o")
  set(NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS "${CMAKE_CURRENT_BINARY_DIR}/Runtime.PrivateLibunwind.localize")
  set(NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS_UNSORTED "${CMAKE_CURRENT_BINARY_DIR}/Runtime.PrivateLibunwind.localize.unsorted")
  # The relocatable object includes runtime-owned objects so their references to
  # libunwind implementation symbols are resolved before localization. The
  # localize list is generated only from the bundled libunwind source objects.
  set(NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOL_OBJECTS "$<FILTER:$<TARGET_OBJECTS:Runtime.PrivateLibunwind>,EXCLUDE,.*(UnwindHelpers|UnixNativeCodeManager)\\.cpp\\.o$>")

  add_custom_command(
    OUTPUT "${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}"
    COMMAND "${CMAKE_COMMAND}" -E rm -f
            "${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}"
            "${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS_UNSORTED}"
    # Generate the objcopy symbol list from libunwind-owned object files only.
    COMMAND sh -c "nm_tool=\"$1\" && shift && \"$nm_tool\" --extern-only --defined-only --format=just-symbols \"$@\" > \"$0\""
            "${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS_UNSORTED}"
            "${CMAKE_NM}"
            ${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOL_OBJECTS}
    COMMAND sort -u -o
            "${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS}"
            "${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS_UNSORTED}"
    COMMAND "${CMAKE_COMMAND}" -E rm -f
            "${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS_UNSORTED}"
    COMMAND "${CMAKE_LINKER}" -r $<TARGET_OBJECTS:Runtime.PrivateLibunwind> -o "${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}"
    COMMAND "${CMAKE_OBJCOPY}"
            "--localize-symbols=${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS}"
            "${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}"
    DEPENDS
      Runtime.PrivateLibunwind
      $<TARGET_OBJECTS:Runtime.PrivateLibunwind>
    BYPRODUCTS "${NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS}"
    COMMENT "Privatizing NativeAOT libunwind symbols"
    COMMAND_EXPAND_LISTS
    VERBATIM
  )
  set_source_files_properties("${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}" PROPERTIES
    EXTERNAL_OBJECT TRUE
    GENERATED TRUE)
endif()

get_compile_definitions(DEFINITIONS)
set(ASM_OFFSETS_CSPP ${NATIVEAOT_RUNTIME_DIR}/../Runtime.Base/src/AsmOffsets.cspp)

if(WIN32)
  set(COMPILER_LANGUAGE "")
  set(PREPROCESSOR_FLAGS -EP -nologo)
  set(ASM_OFFSETS_CPP ${NATIVEAOT_RUNTIME_DIR}/windows/AsmOffsets.cpp)
else()
  set(COMPILER_LANGUAGE -x c++)
  set(PREPROCESSOR_FLAGS -E -P)
  set(ASM_OFFSETS_CPP ${NATIVEAOT_RUNTIME_DIR}/unix/AsmOffsets.cpp)
endif()

if(NOT FEATURE_PORTABLE_HELPERS)
  add_custom_command(
    COMMAND ${CMAKE_CXX_COMPILER} ${DEFINITIONS} ${PREPROCESSOR_FLAGS} -I"${NATIVEAOT_RUNTIME_DIR}" -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
    OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
    DEPENDS "${ASM_OFFSETS_CPP}" "${NATIVEAOT_RUNTIME_DIR}/AsmOffsets.h"
    COMMENT "Generating AsmOffsets.inc"
  )

  set_source_files_properties("${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc" PROPERTIES GENERATED TRUE)

  # Both runtime archives share AsmOffsets.inc and the assembler helpers.
  add_custom_target(
    RuntimeAsmHelpers
    DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
  )

  add_library(Runtime.AsmHelpers OBJECT ${RUNTIME_SOURCES_ARCH_ASM})
  add_dependencies(Runtime.AsmHelpers RuntimeAsmHelpers)
endif()

add_library(Runtime.GC.Workstation OBJECT ${GC_WKS_SVR_SOURCES})
if(FEATURE_PERFTRACING)
  add_dependencies(Runtime.GC.Workstation aot_eventing_headers)
endif()
set_target_properties(Runtime.GC.Workstation PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)
if(CLR_CMAKE_TARGET_WIN32)
  # GC objects are archived into the runtime libraries and must not reference an external compiler PDB.
  set_target_properties(Runtime.GC.Workstation PROPERTIES MSVC_DEBUG_INFORMATION_FORMAT Embedded)
endif()

add_library(Runtime.WorkstationGC STATIC
  ${COMMON_RUNTIME_SOURCES}
  ${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}
  $<TARGET_OBJECTS:Runtime.GC.Workstation>)
if(FEATURE_PERFTRACING)
  add_dependencies(Runtime.WorkstationGC aot_eventing_headers)
endif()
target_link_libraries(Runtime.WorkstationGC PRIVATE aotminipal)
if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  target_link_libraries(Runtime.WorkstationGC PRIVATE nativeaot_cdac_contract_descriptor nativeaot_gc_wks_descriptor)
endif()
if(NOT FEATURE_PORTABLE_HELPERS)
  target_link_libraries(Runtime.WorkstationGC PRIVATE Runtime.AsmHelpers)
endif()
install_static_library(Runtime.WorkstationGC aotsdk nativeaot)

if(FEATURE_MULTITHREADING AND NOT CLR_CMAKE_TARGET_ARCH_WASM)
  add_library(Runtime.GC.Server OBJECT ${GC_WKS_SVR_SOURCES})
  if(FEATURE_PERFTRACING)
    add_dependencies(Runtime.GC.Server aot_eventing_headers)
  endif()
  target_compile_definitions(Runtime.GC.Server PRIVATE FEATURE_SVR_GC SERVER_GC)
  set_target_properties(Runtime.GC.Server PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)
  if(CLR_CMAKE_TARGET_WIN32)
    set_target_properties(Runtime.GC.Server PROPERTIES MSVC_DEBUG_INFORMATION_FORMAT Embedded)
  endif()

  add_library(Runtime.ServerGC STATIC
    ${COMMON_RUNTIME_SOURCES}
    ${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}
    $<TARGET_OBJECTS:Runtime.GC.Workstation>
    $<TARGET_OBJECTS:Runtime.GC.Server>)
  if(FEATURE_PERFTRACING)
    add_dependencies(Runtime.ServerGC aot_eventing_headers)
  endif()
  target_link_libraries(Runtime.ServerGC PRIVATE aotminipal nativeaot_cdac_contract_descriptor nativeaot_gc_wks_descriptor nativeaot_gc_svr_descriptor Runtime.AsmHelpers)
  target_compile_definitions(Runtime.ServerGC PRIVATE FEATURE_SVR_GC)
  install_static_library(Runtime.ServerGC aotsdk nativeaot)
endif()

add_library(standalonegc-disabled STATIC ${STANDALONEGC_DISABLED_SOURCES})
if(FEATURE_PERFTRACING)
  add_dependencies(standalonegc-disabled aot_eventing_headers)
endif()
if(FEATURE_EVENT_TRACE AND CLR_CMAKE_TARGET_WIN32)
  add_dependencies(standalonegc-disabled aot_etw_headers)
endif()
install_static_library(standalonegc-disabled aotsdk nativeaot)

if(NOT CLR_CMAKE_TARGET_ARCH_WASM)
  add_library(standalonegc-enabled STATIC ${STANDALONEGC_ENABLED_SOURCES})
  if(FEATURE_PERFTRACING)
    add_dependencies(standalonegc-enabled aot_eventing_headers)
  endif()
  if(FEATURE_EVENT_TRACE AND CLR_CMAKE_TARGET_WIN32)
    add_dependencies(standalonegc-enabled aot_etw_headers)
  endif()
  install_static_library(standalonegc-enabled aotsdk nativeaot)
endif()

if(CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  add_library(Runtime.VxsortEnabled STATIC ${VXSORT_SOURCES})
  add_library(Runtime.VxsortDisabled STATIC ${DUMMY_VXSORT_SOURCES})
  install_static_library(Runtime.VxsortEnabled aotsdk nativeaot)
  install_static_library(Runtime.VxsortDisabled aotsdk nativeaot)
endif()

if(CLR_CMAKE_TARGET_WIN32)
  set_target_properties(aotminipal PROPERTIES
    COMPILE_PDB_NAME "aotminipal"
    COMPILE_PDB_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>")
endif()
install_static_library(aotminipal aotsdk nativeaot)

add_custom_command(
  # AsmOffsets.cs is consumed later by the managed build.
  TARGET Runtime.WorkstationGC
  POST_BUILD
  COMMAND ${CMAKE_CXX_COMPILER} ${COMPILER_LANGUAGE} ${DEFINITIONS} ${PREPROCESSOR_FLAGS}
          -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CSPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.cs"
)
