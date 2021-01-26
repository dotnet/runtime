set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(RUNTIMEINFO_SOURCES
    runtimeinfo.cpp
)

add_library_clr(runtimeinfo STATIC ${RUNTIMEINFO_SOURCES})

add_dependencies(runtimeinfo coreclr_module_index_header)

if (NOT (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_I386 OR CLR_CMAKE_TARGET_ARCH_ARM) AND CLR_CMAKE_HOST_ARCH_AMD64))
    add_dependencies(runtimeinfo mscordaccore_module_index_header)
    add_dependencies(runtimeinfo mscordbi_module_index_header)
endif()

# publish runtimeinfo lib
_install(TARGETS runtimeinfo DESTINATION lib)
