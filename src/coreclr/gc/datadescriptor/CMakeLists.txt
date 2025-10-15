# cDAC GC contract descriptor

# Up to four separate descriptors can be generated:
# 1. gc_dll_wks_descriptor - WKS GC contract descriptor for the GC DLL
# 2. gc_dll_svr_descriptor - SVR GC contract descriptor for the GC DLL (if FEATURE_SVR_GC is enabled)
# 3. gcexp_dll_wks_descriptor - WKS GC contract descriptor for the GC EXP DLL (if BUILD_EXP_GC is enabled)
# 4. gcexp_dll_svr_descriptor - SVR GC contract descriptor for the GC EXP DLL (if BUILD_EXP_GC and FEATURE_SVR_GC are enabled)

add_library(gc_dll_wks_descriptor_interface INTERFACE)
target_include_directories(gc_dll_wks_descriptor_interface INTERFACE
    ${CMAKE_CURRENT_SOURCE_DIR})
add_dependencies(gc_dll_wks_descriptor_interface eventing_headers)
generate_data_descriptors(
    LIBRARY_NAME gc_dll_wks_descriptor
    CONTRACT_NAME "GCContractDescriptorWKS"
    INTERFACE_TARGET gc_dll_wks_descriptor_interface)

if (FEATURE_SVR_GC)
  add_library(gc_dll_svr_descriptor_interface INTERFACE)
  target_include_directories(gc_dll_svr_descriptor_interface INTERFACE
      ${CMAKE_CURRENT_SOURCE_DIR})
  add_dependencies(gc_dll_svr_descriptor_interface eventing_headers)
  target_compile_definitions(gc_dll_svr_descriptor_interface INTERFACE -DSERVER_GC)
  generate_data_descriptors(
      LIBRARY_NAME gc_dll_svr_descriptor
      CONTRACT_NAME "GCContractDescriptorSVR"
      INTERFACE_TARGET gc_dll_svr_descriptor_interface)
endif()

if(BUILD_EXP_GC)
  add_library(gcexp_dll_wks_descriptor_interface INTERFACE)
  target_include_directories(gcexp_dll_wks_descriptor_interface INTERFACE
      ${CMAKE_CURRENT_SOURCE_DIR})
  add_dependencies(gcexp_dll_wks_descriptor_interface eventing_headers)
  target_compile_definitions(gcexp_dll_wks_descriptor_interface INTERFACE -DUSE_REGIONS)
  generate_data_descriptors(
      LIBRARY_NAME gcexp_dll_wks_descriptor
      CONTRACT_NAME "GCContractDescriptorWKS"
      INTERFACE_TARGET gcexp_dll_wks_descriptor_interface)

  if (FEATURE_SVR_GC)
    add_library(gcexp_dll_svr_descriptor_interface INTERFACE)
    target_include_directories(gcexp_dll_svr_descriptor_interface INTERFACE
        ${CMAKE_CURRENT_SOURCE_DIR})
    add_dependencies(gcexp_dll_svr_descriptor_interface eventing_headers)
    target_compile_definitions(gcexp_dll_svr_descriptor_interface INTERFACE -DUSE_REGIONS)
    target_compile_definitions(gcexp_dll_svr_descriptor_interface INTERFACE -DSERVER_GC)
    generate_data_descriptors(
        LIBRARY_NAME gcexp_dll_svr_descriptor
        CONTRACT_NAME "GCContractDescriptorSVR"
        INTERFACE_TARGET gcexp_dll_svr_descriptor_interface)
  endif()
endif()
