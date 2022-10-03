set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(RUNTIMEINFO_SOURCES
    runtimeinfo.cpp
)

add_library_clr(runtimeinfo STATIC ${RUNTIMEINFO_SOURCES})

function(generate_module_index Target ModuleIndexFile)
    if(CLR_CMAKE_HOST_WIN32)
        set(scriptExt ".cmd")
    else()
        set(scriptExt ".sh")
    endif()

    add_custom_command(
        OUTPUT ${ModuleIndexFile}
        COMMAND ${CLR_ENG_NATIVE_DIR}/genmoduleindex${scriptExt} $<TARGET_FILE:${Target}> ${ModuleIndexFile}
        DEPENDS ${Target}
        COMMENT "Generating ${Target} module index file -> ${ModuleIndexFile}"
    )

    set_source_files_properties(
        ${ModuleIndexFile}
        PROPERTIES GENERATED TRUE
    )

    add_dependencies(runtimeinfo ${Target})
    target_sources(runtimeinfo PRIVATE ${ModuleIndexFile})
endfunction(generate_module_index)

if(NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
    generate_module_index(coreclr ${CMAKE_CURRENT_BINARY_DIR}/runtimemoduleindex.h)
    generate_module_index(mscordaccore ${CMAKE_CURRENT_BINARY_DIR}/dacmoduleindex.h)
    generate_module_index(mscordbi ${CMAKE_CURRENT_BINARY_DIR}/dbimoduleindex.h)
endif()

# publish runtimeinfo lib
install_clr(TARGETS runtimeinfo DESTINATIONS lib COMPONENT runtime)
