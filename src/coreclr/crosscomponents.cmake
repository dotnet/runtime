add_definitions(-DCROSS_COMPILE)

set (CLR_CROSS_COMPONENTS_LIST
    crossgen   
    clrjit
    protojit
)

if(NOT CLR_CMAKE_PLATFORM_LINUX)
    list (APPEND CLR_CROSS_COMPONENTS_LIST
        mcs
        mscordaccore
        mscordbi
        sos
        superpmi
        superpmi-shim-collector
        superpmi-shim-counter
        superpmi-shim-simple
    )
endif()
