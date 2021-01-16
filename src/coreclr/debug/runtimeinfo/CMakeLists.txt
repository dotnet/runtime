set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(RUNTIMEINFO_SOURCES
    runtimeinfo.cpp
)

add_library_clr(runtimeinfo_obj
    OBJECT
    ${RUNTIMEINFO_SOURCES}
)

add_dependencies(runtimeinfo_obj coreclr_module_index_header)

if (NOT (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_ARM) AND CLR_CMAKE_HOST_ARCH_AMD64))
    add_dependencies(runtimeinfo_obj mscordaccore_module_index_header)
    add_dependencies(runtimeinfo_obj mscordbi_module_index_header)
endif()

add_library(runtimeinfo INTERFACE)
target_sources(runtimeinfo INTERFACE $<TARGET_OBJECTS:runtimeinfo_obj>)
