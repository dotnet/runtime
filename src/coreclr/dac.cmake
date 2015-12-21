# Contains the dac build specific definitions. Included by the leaf dac cmake files.

add_definitions(-DDACCESS_COMPILE)
add_definitions(-DFEATURE_ENABLE_HARDWARE_EXCEPTIONS)
if(WIN32)    
    add_definitions(-MT)
endif(WIN32)
remove_definitions(-DPROFILING_SUPPORTED)
add_definitions(-DPROFILING_SUPPORTED_DATA)
