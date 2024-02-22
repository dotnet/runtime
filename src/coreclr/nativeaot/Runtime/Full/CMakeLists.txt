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
    if (CMAKE_GENERATOR MATCHES "Visual Studio")
      # Replaces .asm files in RUNTIME_SOURCES_ARCH_ASM with the corresponding .obj files
      compile_asm(TARGET Runtime.WorkstationGC ASM_FILES ${RUNTIME_SOURCES_ARCH_ASM} OUTPUT_OBJECTS RUNTIME_ARCH_ASM_OBJECTS)
    endif()
  endif()
endif (CLR_CMAKE_TARGET_WIN32)

add_library(Runtime.WorkstationGC STATIC ${COMMON_RUNTIME_SOURCES} ${FULL_RUNTIME_SOURCES} ${RUNTIME_SOURCES_ARCH_ASM} ${RUNTIME_ARCH_ASM_OBJECTS})
add_dependencies(Runtime.WorkstationGC aot_eventing_headers)

add_library(Runtime.ServerGC STATIC ${COMMON_RUNTIME_SOURCES} ${FULL_RUNTIME_SOURCES} ${RUNTIME_SOURCES_ARCH_ASM} ${SERVER_GC_SOURCES} ${RUNTIME_ARCH_ASM_OBJECTS})
add_dependencies(Runtime.ServerGC aot_eventing_headers)

add_library(standalonegc-disabled STATIC ${STANDALONEGC_DISABLED_SOURCES})
add_dependencies(standalonegc-disabled aot_eventing_headers)
add_library(standalonegc-enabled STATIC ${STANDALONEGC_ENABLED_SOURCES})
add_dependencies(standalonegc-enabled aot_eventing_headers)
if(CLR_CMAKE_TARGET_WIN32)
  add_dependencies(standalonegc-disabled aot_etw_headers)
  add_dependencies(standalonegc-enabled aot_etw_headers)
endif()

if (CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)
  add_library(Runtime.VxsortEnabled STATIC ${VXSORT_SOURCES})
  add_library(Runtime.VxsortDisabled STATIC ${DUMMY_VXSORT_SOURCES})
endif (CLR_CMAKE_TARGET_WIN32 AND CLR_CMAKE_TARGET_ARCH_AMD64)

target_compile_definitions(Runtime.ServerGC PRIVATE -DFEATURE_SVR_GC)

if (CLR_CMAKE_TARGET_WIN32)
  add_library(standalonegc-disabled.GuardCF STATIC ${STANDALONEGC_DISABLED_SOURCES})
  set_target_properties(standalonegc-disabled.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
  add_dependencies(standalonegc-disabled.GuardCF aot_eventing_headers)
  add_dependencies(standalonegc-disabled.GuardCF aot_etw_headers)
  add_library(standalonegc-enabled.GuardCF STATIC ${STANDALONEGC_ENABLED_SOURCES})
  set_target_properties(standalonegc-enabled.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
  add_dependencies(standalonegc-enabled.GuardCF aot_eventing_headers)
  add_dependencies(standalonegc-enabled.GuardCF aot_etw_headers)
  add_library(Runtime.ServerGC.GuardCF STATIC ${COMMON_RUNTIME_SOURCES} ${FULL_RUNTIME_SOURCES} ${RUNTIME_SOURCES_ARCH_ASM} ${SERVER_GC_SOURCES} ${RUNTIME_ARCH_ASM_OBJECTS})
  target_compile_definitions(Runtime.ServerGC.GuardCF PRIVATE -DFEATURE_SVR_GC)
  set_target_properties(Runtime.ServerGC.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)

  if (CLR_CMAKE_TARGET_ARCH_AMD64)
    add_library(Runtime.VxsortEnabled.GuardCF STATIC ${VXSORT_SOURCES})
    set_target_properties(Runtime.VxsortEnabled.GuardCF PROPERTIES CLR_CONTROL_FLOW_GUARD ON)
  endif (CLR_CMAKE_TARGET_ARCH_AMD64)
endif (CLR_CMAKE_TARGET_WIN32)

# Get the current list of definitions
get_compile_definitions(DEFINITIONS)

set(ASM_OFFSETS_CSPP ${RUNTIME_DIR}/../Runtime.Base/src/AsmOffsets.cspp)

if(WIN32)
    set(COMPILER_LANGUAGE "")
    set(PREPROCESSOR_FLAGS -EP -nologo)
    set(ASM_OFFSETS_CPP ${RUNTIME_DIR}/windows/AsmOffsets.cpp)
else()
    set(COMPILER_LANGUAGE -x c++)
    set(PREPROCESSOR_FLAGS -E -P)
    set(ASM_OFFSETS_CPP ${RUNTIME_DIR}/unix/AsmOffsets.cpp)
endif()

add_custom_command(
    # The AsmOffsets.cs is consumed later by the managed build
    TARGET Runtime.WorkstationGC
    COMMAND ${CMAKE_CXX_COMPILER} ${COMPILER_LANGUAGE} ${DEFINITIONS} ${PREPROCESSOR_FLAGS} -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CSPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.cs"
    DEPENDS "${RUNTIME_DIR}/AsmOffsets.cpp" "${RUNTIME_DIR}/AsmOffsets.h"
)

add_custom_command(
    COMMAND ${CMAKE_CXX_COMPILER} ${DEFINITIONS} ${PREPROCESSOR_FLAGS} -I"${RUNTIME_DIR}" -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
    OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc"
    DEPENDS "${ASM_OFFSETS_CPP}" "${RUNTIME_DIR}/AsmOffsets.h"
    COMMENT "Generating AsmOffsets.inc"
)

set_source_files_properties("${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc" PROPERTIES GENERATED TRUE)

# Runtime.WorkstationGC and Runtime.ServerGC share AsmOffsets.inc and assembler helpers (for Windows ARM/ARM64).
# Avoid a race condition by adding this target as a dependency for both libraries.
add_custom_target(
  RuntimeAsmHelpers
  DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc" "${RUNTIME_ARCH_ASM_OBJECTS}"
)

add_dependencies(Runtime.WorkstationGC RuntimeAsmHelpers)
add_dependencies(Runtime.ServerGC RuntimeAsmHelpers)
if (CLR_CMAKE_TARGET_WIN32)
  add_dependencies(Runtime.ServerGC.GuardCF RuntimeAsmHelpers)
endif (CLR_CMAKE_TARGET_WIN32)

install_static_library(Runtime.WorkstationGC aotsdk nativeaot)
install_static_library(Runtime.ServerGC aotsdk nativeaot)
install_static_library(standalonegc-disabled aotsdk nativeaot)
install_static_library(standalonegc-enabled aotsdk nativeaot)
if (CLR_CMAKE_TARGET_WIN32)
  if (CLR_CMAKE_TARGET_ARCH_AMD64)
    install_static_library(Runtime.VxsortEnabled aotsdk nativeaot)
    install_static_library(Runtime.VxsortDisabled aotsdk nativeaot)
    install_static_library(Runtime.VxsortEnabled.GuardCF aotsdk nativeaot)
  endif (CLR_CMAKE_TARGET_ARCH_AMD64)
  install_static_library(Runtime.ServerGC.GuardCF aotsdk nativeaot)
  add_dependencies(Runtime.ServerGC.GuardCF aot_eventing_headers)
  install_static_library(standalonegc-disabled.GuardCF aotsdk nativeaot)
  install_static_library(standalonegc-enabled.GuardCF aotsdk nativeaot)
endif (CLR_CMAKE_TARGET_WIN32)
