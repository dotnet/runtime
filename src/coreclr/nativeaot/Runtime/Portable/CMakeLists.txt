project(PortableRuntime)

# Portable version of the runtime is designed to be used with CppCodeGen or WASM.
# It should be written in pure C/C++, with no assembly code.

add_definitions(-DUSE_PORTABLE_HELPERS)

add_library(PortableRuntime STATIC ${COMMON_RUNTIME_SOURCES} ${PORTABLE_RUNTIME_SOURCES})

# Get the current list of definitions
get_compile_definitions(DEFINITIONS)
set(ASM_OFFSETS_CSPP ${RUNTIME_DIR}/../Runtime.Base/src/AsmOffsets.cspp)

if(WIN32)
    set(COMPILER_LANGUAGE "")
    set(PREPROCESSOR_FLAGS -EP)
    set(ASM_OFFSETS_CPP ${RUNTIME_DIR}/windows/AsmOffsets.cpp)
else()
    set(COMPILER_LANGUAGE -x c++)
    set(PREPROCESSOR_FLAGS -E -P)
    set(ASM_OFFSETS_CPP ${RUNTIME_DIR}/unix/AsmOffsets.cpp)
endif()

add_custom_command(
   # The AsmOffsetsPortable.cs is consumed later by the managed build
   TARGET PortableRuntime
   COMMAND ${CMAKE_CXX_COMPILER} ${COMPILER_LANGUAGE} ${DEFINITIONS} ${PREPROCESSOR_FLAGS} -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CSPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsetsPortable.cs"
   DEPENDS "${RUNTIME_DIR}/AsmOffsets.cpp" "${RUNTIME_DIR}/AsmOffsets.h"
)

install_static_library(PortableRuntime aotsdk nativeaot)
