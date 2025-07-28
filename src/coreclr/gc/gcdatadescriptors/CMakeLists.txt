# cDAC contract descriptor
generate_data_descriptors(
    LIBRARY_NAME gc_contract_descriptor
    CONTRACT_FILE "${CMAKE_CURRENT_SOURCE_DIR}/contracts.jsonc"
    INCLUDE_DIRS "${CMAKE_CURRENT_SOURCE_DIR}" "${CLR_DIR}/pal/inc" "${CLR_DIR}/inc" "${CLR_DIR}/gc"
    POINTER_DATA_NAME "GCContractPointerData"
    CONTRACT_NAME "GCContractDescriptor")
