project(Runtime)

# Full version of the runtime is required by the JIT CodeGen.
# The low-level helpers can be implemented in assembly code.

# Include auto-generated files on include path
set(CMAKE_INCLUDE_CURRENT_DIR ON)

if (CLR_CMAKE_TARGET_APPLE)
  list(APPEND RUNTIME_SOURCES_ARCH_ASM
    ${ARCH_SOURCES_DIR}/ThunkPoolThunks.${ASM_SUFFIX}
  )
else()
  add_definitions(-DFEATURE_RX_THUNKS)
endif()

if (CLR_CMAKE_TARGET_WIN32)
  if (CLR_CMAKE_HOST_ARCH_ARM OR CLR_CMAKE_HOST_ARCH_ARM64)
    # Needed to include AsmOffsets.inc
    include_directories(BEFORE ${CMAKE_CURRENT_BINARY_DIR})
    preprocess_files(RUNTIME_SOURCES_ARCH_ASM ${RUNTIME_SOURCES_ARCH_ASM})
  endif()
endif (CLR_CMAKE_TARGET_WIN32)

if(NATIVEAOT_PRIVATE_LIBUNWIND_SOURCES)
  add_library(Runtime.PrivateLibunwind OBJECT ${NATIVEAOT_PRIVATE_LIBUNWIND_SOURCES})

  set(NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT "${CMAKE_CURRENT_BINARY_DIR}/Runtime.PrivateLibunwind.o")
  set(NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS "${CMAKE_CURRENT_BINARY_DIR}/Runtime.PrivateLibunwind.localize")
  set(NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOLS_UNSORTED "${CMAKE_CURRENT_BINARY_DIR}/Runtime.PrivateLibunwind.localize.unsorted")
  # The relocatable object includes runtime-owned objects so their references to
  # libunwind implementation symbols are resolved before localization. The
  # localize list is generated only from the bundled libunwind source objects.
  set(NATIVEAOT_PRIVATE_LIBUNWIND_SYMBOL_OBJECTS "$<FILTER:$<TARGET_OBJECTS:Runtime.PrivateLibunwind>,EXCLUDE,.*(UnwindHelpers|UnixNativeCodeManager)\\.cpp\\.o$>")
  set(NATIVEAOT_PRIVATE_LIBUNWIND_LINKER "${CMAKE_LINKER}")
  get_filename_component(NATIVEAOT_PRIVATE_LIBUNWIND_LINKER_NAME "${NATIVEAOT_PRIVATE_LIBUNWIND_LINKER}" NAME)
  if(NATIVEAOT_PRIVATE_LIBUNWIND_LINKER_NAME MATCHES "^llvm-link(-[0-9.]+)?(\\.exe)?$")
    set(NATIVEAOT_PRIVATE_LIBUNWIND_LINKER_VERSION "${CMAKE_MATCH_1}")
    set(NATIVEAOT_PRIVATE_LIBUNWIND_LINKER_EXTENSION "${CMAKE_MATCH_2}")
    get_filename_component(NATIVEAOT_PRIVATE_LIBUNWIND_TOOL_DIR "${NATIVEAOT_PRIVATE_LIBUNWIND_LINKER}" DIRECTORY)
    find_program(NATIVEAOT_PRIVATE_LIBUNWIND_LD_LLD
      NAMES "ld.lld${NATIVEAOT_PRIVATE_LIBUNWIND_LINKER_VERSION}${NATIVEAOT_PRIVATE_LIBUNWIND_LINKER_EXTENSION}" ld.lld
      PATHS "${NATIVEAOT_PRIVATE_LIBUNWIND_TOOL_DIR}"
      NO_DEFAULT_PATH)
    if(NATIVEAOT_PRIVATE_LIBUNWIND_LD_LLD)
      set(NATIVEAOT_PRIVATE_LIBUNWIND_LINKER "${NATIVEAOT_PRIVATE_LIBUNWIND_LD_LLD}")
    else()
      find_program(NATIVEAOT_PRIVATE_LIBUNWIND_LD NAMES ld)
      if(NATIVEAOT_PRIVATE_LIBUNWIND_LD)
        set(NATIVEAOT_PRIVATE_LIBUNWIND_LINKER "${NATIVEAOT_PRIVATE_LIBUNWIND_LD}")
      else()
        message(FATAL_ERROR "ld.lld (preferred) or ld is required to privatize NativeAOT libunwind symbols")
      endif()
    endif()
  endif()

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
    COMMAND "${NATIVEAOT_PRIVATE_LIBUNWIND_LINKER}" -r $<TARGET_OBJECTS:Runtime.PrivateLibunwind> -o "${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}"
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

add_library(Runtime.GC.Workstation OBJECT ${GC_WKS_SVR_SOURCES})
if(FEATURE_PERFTRACING)
  add_dependencies(Runtime.GC.Workstation aot_eventing_headers)
endif()
set_target_properties(Runtime.GC.Workstation PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)

add_library(Runtime.GC.Server OBJECT ${GC_WKS_SVR_SOURCES})
if(FEATURE_PERFTRACING)
  add_dependencies(Runtime.GC.Server aot_eventing_headers)
endif()
target_compile_definitions(Runtime.GC.Server PRIVATE FEATURE_SVR_GC SERVER_GC)
set_target_properties(Runtime.GC.Server PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)

if (CLR_CMAKE_TARGET_WIN32)
  # Embed debug info in the object files (/Z7) rather than referencing an external
  # compiler PDB. CMake does not assign a compile PDB to OBJECT libraries, so the
  # objects archived into Runtime.WorkstationGC/Runtime.ServerGC would otherwise
  # reference a vc140.pdb that is absent when these static libs are statically
  # linked by the AOT compiler publish projects, producing LNK4099.
  target_compile_options(Runtime.GC.Workstation PRIVATE /Z7)
  target_compile_options(Runtime.GC.Server PRIVATE /Z7)
endif (CLR_CMAKE_TARGET_WIN32)

add_library(Runtime.WorkstationGC STATIC
    ${COMMON_RUNTIME_SOURCES}
    ${FULL_RUNTIME_SOURCES}
    ${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}
    ${RUNTIME_ARCH_ASM_OBJECTS}
    $<TARGET_OBJECTS:Runtime.GC.Workstation>)
if(FEATURE_PERFTRACING)
  add_dependencies(Runtime.WorkstationGC aot_eventing_headers)
endif()
target_link_libraries(Runtime.WorkstationGC PRIVATE aotminipal nativeaot_cdac_contract_descriptor nativeaot_gc_wks_descriptor)

add_library(Runtime.ServerGC STATIC
    ${COMMON_RUNTIME_SOURCES}
    ${FULL_RUNTIME_SOURCES}
    ${NATIVEAOT_PRIVATE_LIBUNWIND_OBJECT}
    ${RUNTIME_ARCH_ASM_OBJECTS}
    $<TARGET_OBJECTS:Runtime.GC.Workstation>
    $<TARGET_OBJECTS:Runtime.GC.Server>)
if(FEATURE_PERFTRACING)
  add_dependencies(Runtime.ServerGC aot_eventing_headers)
endif()
target_link_libraries(Runtime.ServerGC PRIVATE aotminipal nativeaot_cdac_contract_descriptor nativeaot_gc_wks_descriptor nativeaot_gc_svr_descriptor)
add_library(standalonegc-disabled STATIC ${STANDALONEGC_DISABLED_SOURCES})
if(FEATURE_PERFTRACING)
  add_dependencies(standalonegc-disabled aot_eventing_headers)
endif()
add_library(standalonegc-enabled STATIC ${STANDALONEGC_ENABLED_SOURCES})
if(FEATURE_PERFTRACING)
  add_dependencies(standalonegc-enabled aot_eventing_headers)
endif()
if(FEATURE_EVENT_TRACE)
  if(CLR_CMAKE_TARGET_WIN32)
    add_dependencies(standalonegc-disabled aot_etw_headers)
    add_dependencies(standalonegc-enabled aot_etw_headers)
  endif()
endif()

if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  add_library(Runtime.VxsortEnabled STATIC ${VXSORT_SOURCES})
  add_library(Runtime.VxsortDisabled STATIC ${DUMMY_VXSORT_SOURCES})
endif (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)

target_compile_definitions(Runtime.ServerGC PRIVATE FEATURE_SVR_GC)

if (CLR_CMAKE_TARGET_WIN32)
  set_target_properties(aotminipal PROPERTIES
    COMPILE_PDB_NAME "aotminipal"
    COMPILE_PDB_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>")
endif (CLR_CMAKE_TARGET_WIN32)

# Get the current list of definitions
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

add_custom_command(
    # The AsmOffsets.cs is consumed later by the managed build
    TARGET Runtime.WorkstationGC
    POST_BUILD
    COMMAND ${CMAKE_CXX_COMPILER} ${COMPILER_LANGUAGE} ${DEFINITIONS} ${PREPROCESSOR_FLAGS}
            -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CSPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.cs"
)

add_custom_command(
    COMMAND ${CMAKE_CXX_COMPILER} ${DEFINITIONS} ${PREPROCESSOR_FLAGS} -I"${NATIVEAOT_RUNTIME_DIR}" -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
    OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
    DEPENDS "${ASM_OFFSETS_CPP}" "${NATIVEAOT_RUNTIME_DIR}/AsmOffsets.h"
    COMMENT "Generating AsmOffsets.inc"
)

set_source_files_properties("${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc" PROPERTIES GENERATED TRUE)

# Runtime.WorkstationGC and Runtime.ServerGC share AsmOffsets.inc and assembler helpers (for Windows ARM/ARM64).
# Avoid a race condition by adding this target as a dependency for both libraries.
add_custom_target(
  RuntimeAsmHelpers
  DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
)

add_library(Runtime.AsmHelpers OBJECT ${RUNTIME_SOURCES_ARCH_ASM})
add_dependencies(Runtime.AsmHelpers RuntimeAsmHelpers)

target_link_libraries(Runtime.WorkstationGC PRIVATE Runtime.AsmHelpers)
target_link_libraries(Runtime.ServerGC PRIVATE Runtime.AsmHelpers)

install_static_library(Runtime.WorkstationGC aotsdk nativeaot)
install_static_library(Runtime.ServerGC aotsdk nativeaot)
install_static_library(standalonegc-disabled aotsdk nativeaot)
install_static_library(standalonegc-enabled aotsdk nativeaot)
install_static_library(aotminipal aotsdk nativeaot)
if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  install_static_library(Runtime.VxsortEnabled aotsdk nativeaot)
  install_static_library(Runtime.VxsortDisabled aotsdk nativeaot)
endif (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
