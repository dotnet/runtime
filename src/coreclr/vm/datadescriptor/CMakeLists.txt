# cDAC contract descriptor

if(CDAC_BUILD_TOOL_BINARY_PATH AND "${CLR_DOTNET_RID}" STREQUAL "")
  message(FATAL_ERROR "CLR_DOTNET_RID is not set. Please ensure it is being set to the portable RID of the target platform by runtime.proj.")
endif()
configure_file(configure.h.in ${CMAKE_CURRENT_BINARY_DIR}/configure.h)

add_library(runtime_descriptor_interface INTERFACE)
target_include_directories(runtime_descriptor_interface INTERFACE
    ${CMAKE_CURRENT_SOURCE_DIR}
    ${CMAKE_CURRENT_BINARY_DIR})
add_dependencies(runtime_descriptor_interface cee_wks_core)
generate_data_descriptors(
   LIBRARY_NAME cdac_contract_descriptor
   CONTRACT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/contracts.jsonc"
   CONTRACT_NAME "DotNetRuntimeContractDescriptor"
   INTERFACE_TARGET runtime_descriptor_interface
   EXPORT_VISIBLE)
