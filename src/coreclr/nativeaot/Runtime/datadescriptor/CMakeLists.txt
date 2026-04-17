set(CMAKE_INCLUDE_CURRENT_DIR OFF)

# cDAC contract descriptor for NativeAOT
#
# This uses the shared datadescriptor infrastructure from
# src/coreclr/debug/datadescriptor-shared/ to generate a
# DotNetRuntimeContractDescriptor symbol in the NativeAOT runtime.
#
# Include directories and compile definitions are inherited from the parent
# Runtime/CMakeLists.txt directory scope. The interface targets below only
# need to add the datadescriptor-specific include path.

include(${CLR_DIR}/clrdatadescriptors.cmake)

add_library(nativeaot_descriptor_interface INTERFACE)
target_include_directories(nativeaot_descriptor_interface INTERFACE
    ${CMAKE_CURRENT_SOURCE_DIR})
target_compile_definitions(nativeaot_descriptor_interface INTERFACE SKIP_TRACING_DEFINITIONS)
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
    ${GC_DESCRIPTOR_DIR}
    ${GC_DIR})
generate_data_descriptors(
    LIBRARY_NAME nativeaot_gc_wks_descriptor
    CONTRACT_NAME "GCContractDescriptorWKS"
    INTERFACE_TARGET nativeaot_gc_wks_descriptor_interface)

add_library(nativeaot_gc_svr_descriptor_interface INTERFACE)
target_include_directories(nativeaot_gc_svr_descriptor_interface INTERFACE
    ${GC_DESCRIPTOR_DIR}
    ${GC_DIR})
target_compile_definitions(nativeaot_gc_svr_descriptor_interface INTERFACE -DSERVER_GC)
generate_data_descriptors(
    LIBRARY_NAME nativeaot_gc_svr_descriptor
    CONTRACT_NAME "GCContractDescriptorSVR"
    INTERFACE_TARGET nativeaot_gc_svr_descriptor_interface)
