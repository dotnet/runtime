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
target_link_libraries(Runtime.WorkstationGC PRIVATE aotminipal)

add_library(Runtime.ServerGC STATIC ${COMMON_RUNTIME_SOURCES} ${FULL_RUNTIME_SOURCES} ${RUNTIME_SOURCES_ARCH_ASM} ${SERVER_GC_SOURCES} ${RUNTIME_ARCH_ASM_OBJECTS})
add_dependencies(Runtime.ServerGC aot_eventing_headers)
target_link_libraries(Runtime.ServerGC PRIVATE aotminipal)

add_library(standalonegc-disabled STATIC ${STANDALONEGC_DISABLED_SOURCES})
add_dependencies(standalonegc-disabled aot_eventing_headers)
add_library(standalonegc-enabled STATIC ${STANDALONEGC_ENABLED_SOURCES})
add_dependencies(standalonegc-enabled aot_eventing_headers)
if(CLR_CMAKE_TARGET_WIN32)
  add_dependencies(standalonegc-disabled aot_etw_headers)
  add_dependencies(standalonegc-enabled aot_etw_headers)
endif()

if (CLR_CMAKE_TARGET_ARCH_AMD64)
  add_library(Runtime.VxsortEnabled STATIC ${VXSORT_SOURCES})
  add_library(Runtime.VxsortDisabled STATIC ${DUMMY_VXSORT_SOURCES})
endif (CLR_CMAKE_TARGET_ARCH_AMD64)

target_compile_definitions(Runtime.ServerGC PRIVATE -DFEATURE_SVR_GC)

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
  DEPENDS "${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.inc" "${RUNTIME_ARCH_ASM_OBJECTS}"
)

add_dependencies(Runtime.WorkstationGC RuntimeAsmHelpers)
add_dependencies(Runtime.ServerGC RuntimeAsmHelpers)

install_static_library(Runtime.WorkstationGC aotsdk nativeaot)
install_static_library(Runtime.ServerGC aotsdk nativeaot)
install_static_library(standalonegc-disabled aotsdk nativeaot)
install_static_library(standalonegc-enabled aotsdk nativeaot)
install_static_library(aotminipal aotsdk nativeaot)
if (CLR_CMAKE_TARGET_ARCH_AMD64)
  install_static_library(Runtime.VxsortEnabled aotsdk nativeaot)
  install_static_library(Runtime.VxsortDisabled aotsdk nativeaot)
endif (CLR_CMAKE_TARGET_ARCH_AMD64)
