project(PortableRuntime)

# Portable version of the runtime is designed to be used with WASM.
# It should be written in pure C/C++, with no assembly code.

add_definitions(-DFEATURE_PORTABLE_HELPERS)

add_library(Runtime.GC.Workstation OBJECT ${GC_WKS_SVR_SOURCES})
set_target_properties(Runtime.GC.Workstation PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)

add_library(PortableRuntime STATIC
    ${COMMON_RUNTIME_SOURCES}
    ${PORTABLE_RUNTIME_SOURCES}
    ${NATIVEAOT_RUNTIME_DIR}/AsmOffsetsVerify.cpp
    $<TARGET_OBJECTS:Runtime.GC.Workstation>)
target_link_libraries(PortableRuntime PRIVATE aotminipal)

add_library(standalonegc-disabled STATIC ${STANDALONEGC_DISABLED_SOURCES})

# Get the current list of definitions
get_compile_definitions(DEFINITIONS)
set(ASM_OFFSETS_CSPP ${NATIVEAOT_RUNTIME_DIR}/../Runtime.Base/src/AsmOffsets.cspp)

if(WIN32)
    set(COMPILER_LANGUAGE "")
    set(PREPROCESSOR_FLAGS -EP -nologo)
    set(ASM_OFFSETS_CPP ${NATIVEAOT_RUNTIME_DIR}/windows/AsmOffsets.cpp)

    set_target_properties(aotminipal PROPERTIES
        COMPILE_PDB_NAME "aotminipal"
        COMPILE_PDB_OUTPUT_DIRECTORY "${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>")
else()
    set(COMPILER_LANGUAGE -x c++)
    set(PREPROCESSOR_FLAGS -E -P)
    set(ASM_OFFSETS_CPP ${NATIVEAOT_RUNTIME_DIR}/unix/AsmOffsets.cpp)
endif()

add_custom_command(
   # The AsmOffsetsPortable.cs is consumed later by the managed build
   TARGET PortableRuntime
   POST_BUILD
   COMMAND ${CMAKE_CXX_COMPILER} ${COMPILER_LANGUAGE} ${DEFINITIONS} ${PREPROCESSOR_FLAGS} -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CSPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsetsPortable.cs"
)

install_static_library(PortableRuntime aotsdk nativeaot)
install_static_library(standalonegc-disabled aotsdk nativeaot)
install_static_library(aotminipal aotsdk nativeaot)
