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
        COMMAND ${CLR_ENG_NATIVE_DIR}/genmoduleindex${scriptExt} $<TARGET_FILE:${Target}> ${ModuleIndexFile} ${CMAKE_READELF}
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


add_library(cdac_data_descriptor OBJECT data-descriptor.c)


set(GENERATED_CDAC_DESCRIPTOR_DIR "${CMAKE_CURRENT_BINARY_DIR}/cdac")
if(CLR_CMAKE_HOST_WIN32)
    set(DOTNET_CMD "${CLR_REPO_ROOT_DIR}/dotnet.cmd")
else()
    set(DOTNET_CMD "${CLR_REPO_ROOT_DIR}/dotnet.sh")
endif()

set(CONTRACT_DESCRIPTOR_OUTPUT "${GENERATED_CDAC_DESCRIPTOR_DIR}/contract-descriptor.c")
set(CDAC_BUILD_TOOL_CSPROJ "${CLR_DIR}/tools/cdac-build-tool/cdac-build-tool.csproj")

set(CONTRACT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/contracts.txt")

add_custom_command(
    OUTPUT "${CONTRACT_DESCRIPTOR_OUTPUT}"
    VERBATIM
    COMMAND "${DOTNET_CMD}" run --project "${CDAC_BUILD_TOOL_CSPROJ}" -c "$<CONFIG>" -- compose -v -o "${CONTRACT_DESCRIPTOR_OUTPUT}" -c "${CONTRACT_FILE}" $<TARGET_OBJECTS:cdac_data_descriptor>
    DEPENDS cdac_data_descriptor $<TARGET_OBJECTS:cdac_data_descriptor> "${CONTRACT_FILE}"
    USES_TERMINAL
)

add_library_clr(cdac_contract_descriptor OBJECT
    "${CONTRACT_DESCRIPTOR_OUTPUT}"
    contract-aux-data.c
)
add_dependencies(cdac_contract_descriptor cdac_data_descriptor)
