include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(BEFORE ${CLR_DIR}/src/unwinder)
include_directories(${CLR_DIR}/src/debug/ee)
include_directories(${CLR_DIR}/src/gc)
include_directories(${CLR_DIR}/src/gcdump)
include_directories(${CLR_DIR}/src/debug/daccess)

if(IS_64BIT_BUILD EQUAL 1)
    include_directories(amd64)

    set(UNWINDER_SOURCES
        unwinder.cpp
        amd64/unwinder_amd64.cpp
    )
    
    convert_to_absolute_path(UNWINDER_SOURCES ${UNWINDER_SOURCES})
        
    add_subdirectory(dac)
endif(IS_64BIT_BUILD EQUAL 1)

