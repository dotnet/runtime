add_definitions(-DCROSS_COMPILE)

set (CLR_CROSS_COMPONENTS_LIST
    crossgen   
    clrjit
    protojit
)

if(NOT CLR_CMAKE_PLATFORM_LINUX)
    list (APPEND CLR_CROSS_COMPONENTS_LIST
        mscordaccore
        mscordbi
        sos
    )
endif()
