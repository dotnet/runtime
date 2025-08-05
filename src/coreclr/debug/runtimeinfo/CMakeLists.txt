set(RUNTIMEINFO_SOURCES
    runtimeinfo.cpp
)

add_library_clr(runtimeinfo STATIC ${RUNTIMEINFO_SOURCES})
target_include_directories(runtimeinfo PRIVATE ${CMAKE_CURRENT_SOURCE_DIR} ${CMAKE_CURRENT_BINARY_DIR})

function(generate_module_index Target ModuleIndexFile)
    # Win32 may be false when cross compiling
    if (CMAKE_HOST_SYSTEM_NAME STREQUAL "Windows")
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


# main vm data descriptor

if(CDAC_BUILD_TOOL_BINARY_PATH AND "${CLR_DOTNET_RID}" STREQUAL "")
  message(FATAL_ERROR "CLR_DOTNET_RID is not set. Please ensure it is being set to the portable RID of the target platform by runtime.proj.")
endif()
configure_file(configure.h.in ${CMAKE_CURRENT_BINARY_DIR}/configure.h)

add_library(runtime_descriptor_interface INTERFACE)
target_include_directories(runtime_descriptor_interface INTERFACE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_BINARY_DIR}
    ${VM_DIR}
    ${VM_DIR}/${ARCH_SOURCES_DIR}
    ${CLR_DIR}/interop/inc)
add_dependencies(runtime_descriptor_interface cee_wks_core)
generate_data_descriptors(
   LIBRARY_NAME cdac_contract_descriptor
   CONTRACT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/contracts.jsonc"
   CONTRACT_NAME "DotNetRuntimeContractDescriptor"
   DLLEXPORT
   INTERFACE_TARGET runtime_descriptor_interface)

set(GC_DESCRIPTOR_DIR "${CLR_DIR}/gc/gcdatadescriptors")
add_library(gc_wks_descriptor_interface INTERFACE)
target_include_directories(gc_wks_descriptor_interface INTERFACE
    ${GC_DESCRIPTOR_DIR}
    ${CLR_DIR}/gc
    ${CLR_DIR}/gc/env)
add_dependencies(gc_wks_descriptor_interface cee_wks_core)
target_compile_definitions(gc_wks_descriptor_interface INTERFACE -DIN_PROCESS_GC)
generate_data_descriptors(
    LIBRARY_NAME gc_wks_descriptor
    CONTRACT_NAME "GCContractDescriptorWKS"
    CONTRACT_FILE "${GC_DESCRIPTOR_DIR}/contracts.jsonc"
    INTERFACE_TARGET gc_wks_descriptor_interface)

if (FEATURE_SVR_GC)
  add_library(gc_svr_descriptor_interface INTERFACE)
  target_include_directories(gc_svr_descriptor_interface INTERFACE
      ${GC_DESCRIPTOR_DIR}
      ${CLR_DIR}/gc
      ${CLR_DIR}/gc/env)
  add_dependencies(gc_svr_descriptor_interface cee_wks_core)
  target_compile_definitions(gc_svr_descriptor_interface INTERFACE -DSERVER_GC)
  target_compile_definitions(gc_svr_descriptor_interface INTERFACE -DIN_PROCESS_GC)
  generate_data_descriptors(
      LIBRARY_NAME gc_svr_descriptor
      CONTRACT_NAME "GCContractDescriptorSVR"
      CONTRACT_FILE "${GC_DESCRIPTOR_DIR}/contracts.jsonc"
      INTERFACE_TARGET gc_svr_descriptor_interface)
endif()
