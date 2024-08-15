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


# cDAC contract descriptor

if (NOT CDAC_BUILD_TOOL_BINARY_PATH)
  # if CDAC_BUILD_TOOL_BINARY_PATH is unspecified (for example for a build without a .NET SDK or msbuild),
  # link a stub contract descriptor into the runtime
  add_library_clr(cdac_contract_descriptor OBJECT contractdescriptorstub.c)
  message(STATUS "Using a stub cDAC contract descriptor")
else()
  # generate a contract descriptor using cdac-build-tool from a data descriptor and contract json file

  add_library(cdac_data_descriptor OBJECT datadescriptor.cpp)
  # don't build the data descriptor before the VM (and any of its dependencies' generated headers)
  add_dependencies(cdac_data_descriptor cee_wks_core)
  if(CLR_CMAKE_TARGET_WIN32)
    # turn off whole program optimization:
    # 1. it creates object files that cdac-build-tool can't read
    # 2. we never link cdac_data_descriptor into the final product - it's only job is to be scraped
    target_compile_options(cdac_data_descriptor PRIVATE /GL-)
  endif()
  target_include_directories(cdac_data_descriptor BEFORE PRIVATE ${VM_DIR})
  target_include_directories(cdac_data_descriptor BEFORE PRIVATE ${VM_DIR}/${ARCH_SOURCES_DIR})
  target_include_directories(cdac_data_descriptor PRIVATE ${CLR_DIR}/interop/inc)

  set(GENERATED_CDAC_DESCRIPTOR_DIR "${CMAKE_CURRENT_BINARY_DIR}/cdac")
  set(CONTRACT_DESCRIPTOR_OUTPUT "${GENERATED_CDAC_DESCRIPTOR_DIR}/contract-descriptor.c")
  if(NOT EXISTS "${CDAC_BUILD_TOOL_BINARY_PATH}")
    message(FATAL_ERROR "${CDAC_BUILD_TOOL_BINARY_PATH} does not exist")
  endif()

  set(CONTRACT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/contracts.jsonc")

  # generate the contract descriptor by running cdac-build-tool
  # n.b. this just uses `dotnet` from the PATH.  InitializeDotNetCli adds the apropropriate directory
  add_custom_command(
    OUTPUT "${CONTRACT_DESCRIPTOR_OUTPUT}"
    VERBATIM
    COMMAND ${CLR_DOTNET_HOST_PATH} ${CDAC_BUILD_TOOL_BINARY_PATH} compose -o "${CONTRACT_DESCRIPTOR_OUTPUT}" -c "${CONTRACT_FILE}" $<TARGET_OBJECTS:cdac_data_descriptor>
    DEPENDS cdac_data_descriptor cee_wks_core $<TARGET_OBJECTS:cdac_data_descriptor> "${CONTRACT_FILE}"
    USES_TERMINAL
  )

  # It is important that cdac_contract_descriptor is an object library;
  # if it was static, linking it into the final dll would not export
  # DotNetRuntimeContractDescriptor since it is not referenced anywhere.
  add_library_clr(cdac_contract_descriptor OBJECT
    "${CONTRACT_DESCRIPTOR_OUTPUT}"
    contractpointerdata.cpp
  )
  target_include_directories(cdac_contract_descriptor BEFORE PRIVATE ${VM_DIR})
  target_include_directories(cdac_contract_descriptor BEFORE PRIVATE ${VM_DIR}/${ARCH_SOURCES_DIR})
  target_include_directories(cdac_contract_descriptor PRIVATE ${CLR_DIR}/interop/inc)
  add_dependencies(cdac_contract_descriptor cdac_data_descriptor cee_wks_core)
endif()
