include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(BEFORE ${CLR_DIR}/src/unwinder)
include_directories(${CLR_DIR}/src/debug/ee)
include_directories(${CLR_DIR}/src/gc)
include_directories(${CLR_DIR}/src/gcdump)
include_directories(${CLR_DIR}/src/debug/daccess)

set(UNWINDER_SOURCES
    unwinder.cpp
)

# Include platform specific unwinder for applicable (native and cross-target) builds.
include_directories(${ARCH_SOURCES_DIR})
list(APPEND UNWINDER_SOURCES
    ${ARCH_SOURCES_DIR}/unwinder_${ARCH_SOURCES_DIR}.cpp
)

convert_to_absolute_path(UNWINDER_SOURCES ${UNWINDER_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CLR_CMAKE_PLATFORM_UNIX)    
    add_subdirectory(wks)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
