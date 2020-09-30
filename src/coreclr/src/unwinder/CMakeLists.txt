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

if(CLR_CMAKE_HOST_UNIX)
    add_library_clr(unwinder_wks_obj OBJECT ${UNWINDER_SOURCES})
    add_dependencies(unwinder_wks_obj eventing_headers)
    add_library(unwinder_wks INTERFACE)
    target_sources(unwinder_wks INTERFACE $<TARGET_OBJECTS:unwinder_wks_obj>)
endif(CLR_CMAKE_HOST_UNIX)

add_library_clr(unwinder_dac ${UNWINDER_SOURCES})
add_dependencies(unwinder_dac eventing_headers)
set_target_properties(unwinder_dac PROPERTIES DAC_COMPONENT TRUE)
target_compile_definitions(unwinder_dac PRIVATE FEATURE_NO_HOST)

