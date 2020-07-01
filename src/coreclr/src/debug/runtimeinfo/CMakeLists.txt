set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(RUNTIMEINFO_SOURCES
    runtimeinfo.cpp
)

add_library_clr(runtimeinfo STATIC ${RUNTIMEINFO_SOURCES})

add_dependencies(runtimeinfo coreclr_module_index_header)
add_dependencies(runtimeinfo mscordaccore_module_index_header)
add_dependencies(runtimeinfo mscordbi_module_index_header)
