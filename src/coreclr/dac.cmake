# Contains the dac build specific definitions. Included by the leaf dac cmake files.

add_definitions(-DDACCESS_COMPILE)
if(WIN32)    
    add_definitions(-MT)
endif(WIN32)

remove_definitions(-DPROFILING_SUPPORTED)
add_definitions(-DPROFILING_SUPPORTED_DATA)

remove_definitions(-DFEATURE_PROFAPI_ATTACH_DETACH)
add_definitions(-DDATA_PROFAPI_ATTACH_DETACH)
