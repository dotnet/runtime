set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(RUNTIMEINFO_SOURCES
    runtimeinfo.cpp
)

add_library_clr(runtimeinfo_lib STATIC ${RUNTIMEINFO_SOURCES})

add_dependencies(runtimeinfo_lib runtime_module_index_header)
add_dependencies(runtimeinfo_lib dac_module_index_header)
add_dependencies(runtimeinfo_lib dbi_module_index_header)
