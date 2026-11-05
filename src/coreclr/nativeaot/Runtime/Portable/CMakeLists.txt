project(Runtime)

# Portable version of the runtime is designed to be used with WASM.
# It should be written in pure C/C++, with no assembly code.

add_definitions(-DFEATURE_PORTABLE_HELPERS)

add_library(Runtime.GC.Workstation OBJECT ${GC_WKS_SVR_SOURCES})
add_dependencies(Runtime.GC.Workstation aot_eventing_headers)
set_target_properties(Runtime.GC.Workstation PROPERTIES UNITY_BUILD ON UNITY_BUILD_BATCH_SIZE 0)

add_library(Runtime.WorkstationGC STATIC
    ${COMMON_RUNTIME_SOURCES}
    ${RUNTIME_DIR}/portable/AllocFast.cpp
    ${PORTABLE_RUNTIME_SOURCES}
    $<TARGET_OBJECTS:Runtime.GC.Workstation>)
target_link_libraries(Runtime.WorkstationGC PRIVATE aotminipal)
add_dependencies(Runtime.WorkstationGC aot_eventing_headers)

add_library(standalonegc-disabled STATIC ${STANDALONEGC_DISABLED_SOURCES})
add_dependencies(standalonegc-disabled aot_eventing_headers)

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
   # The AsmOffsets.cs is consumed later by the managed build
   TARGET Runtime.WorkstationGC
   POST_BUILD
   COMMAND ${CMAKE_CXX_COMPILER} ${COMPILER_LANGUAGE} ${DEFINITIONS} ${PREPROCESSOR_FLAGS} -I"${ARCH_SOURCES_DIR}" "${ASM_OFFSETS_CSPP}" >"${CMAKE_CURRENT_BINARY_DIR}/AsmOffsets.cs"
)

install_static_library(Runtime.WorkstationGC aotsdk nativeaot)
install_static_library(standalonegc-disabled aotsdk nativeaot)
install_static_library(aotminipal aotsdk nativeaot)
