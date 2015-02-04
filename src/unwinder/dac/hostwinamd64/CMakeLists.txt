remove_definitions(-DPROFILING_SUPPORTED)
add_definitions(-DPROFILING_SUPPORTED_DATA)
add_definitions(-DDACCESS_COMPILE)

add_library(unwinder_dac_amd64 ${UNWINDER_SOURCES})
