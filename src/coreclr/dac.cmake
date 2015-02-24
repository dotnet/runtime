# Contains the dac build specific definitions. Included by the leaf dac cmake files.

remove_definitions(-DPROFILING_SUPPORTED)
add_definitions(-DDACCESS_COMPILE)
if(WIN32)
    add_definitions(-MT)
endif(WIN32)