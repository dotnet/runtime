set(CMAKE_INCLUDE_CURRENT_DIR OFF)

# cDAC contract descriptor for NativeAOT
#
# This uses the shared datadescriptor infrastructure from
# src/coreclr/debug/datadescriptor-shared/ to generate a
# DotNetRuntimeContractDescriptor symbol in the NativeAOT runtime.
#
# The nativeaot_runtime_includes interface library (created in the parent
# Runtime/CMakeLists.txt) provides the same include directories used by
# the regular NativeAOT Runtime build.

include(${CLR_DIR}/clrdatadescriptors.cmake)

add_library(nativeaot_descriptor_interface INTERFACE)
target_include_directories(nativeaot_descriptor_interface INTERFACE
    ${CMAKE_CURRENT_SOURCE_DIR})
target_link_libraries(nativeaot_descriptor_interface INTERFACE nativeaot_runtime_includes)
add_dependencies(nativeaot_descriptor_interface aot_eventing_headers)
generate_data_descriptors(
    LIBRARY_NAME nativeaot_cdac_contract_descriptor
    CONTRACT_NAME "DotNetRuntimeContractDescriptor"
    INTERFACE_TARGET nativeaot_descriptor_interface
    EXPORT_VISIBLE)

# GC contract descriptors (workstation + server).
# The GC has its own data descriptor exposed as a sub-descriptor via gc_descriptor in GcDacVars.
set(GC_DESCRIPTOR_DIR "${CLR_DIR}/gc/datadescriptor")

add_library(nativeaot_gc_wks_descriptor_interface INTERFACE)
target_include_directories(nativeaot_gc_wks_descriptor_interface INTERFACE
    ${GC_DESCRIPTOR_DIR})
target_link_libraries(nativeaot_gc_wks_descriptor_interface INTERFACE nativeaot_runtime_includes)
add_dependencies(nativeaot_gc_wks_descriptor_interface aot_eventing_headers)
generate_data_descriptors(
    LIBRARY_NAME nativeaot_gc_wks_descriptor
    CONTRACT_NAME "GCContractDescriptorWKS"
    INTERFACE_TARGET nativeaot_gc_wks_descriptor_interface)

add_library(nativeaot_gc_svr_descriptor_interface INTERFACE)
target_include_directories(nativeaot_gc_svr_descriptor_interface INTERFACE
    ${GC_DESCRIPTOR_DIR})
target_link_libraries(nativeaot_gc_svr_descriptor_interface INTERFACE nativeaot_runtime_includes)
add_dependencies(nativeaot_gc_svr_descriptor_interface aot_eventing_headers)
target_compile_definitions(nativeaot_gc_svr_descriptor_interface INTERFACE -DSERVER_GC)
generate_data_descriptors(
    LIBRARY_NAME nativeaot_gc_svr_descriptor
    CONTRACT_NAME "GCContractDescriptorSVR"
    INTERFACE_TARGET nativeaot_gc_svr_descriptor_interface)
