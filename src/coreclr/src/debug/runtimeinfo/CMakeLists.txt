set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(RUNTIMEINFO_SOURCES
    runtimeinfo.cpp
)

add_library_clr(runtimeinfo STATIC ${RUNTIMEINFO_SOURCES})

add_dependencies(runtimeinfo runtime_module_index_header)
add_dependencies(runtimeinfo dac_module_index_header)
add_dependencies(runtimeinfo dbi_module_index_header)
