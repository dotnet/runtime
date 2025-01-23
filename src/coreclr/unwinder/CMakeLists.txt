include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(BEFORE ${CLR_DIR}/unwinder)
include_directories(${CLR_DIR}/debug/ee)
include_directories(${CLR_DIR}/gc)
include_directories(${CLR_DIR}/gcdump)
include_directories(${CLR_DIR}/debug/daccess)

set(UNWINDER_SOURCES
    baseunwinder.cpp
)

# Include platform specific unwinder for applicable (native and cross-target) builds.
include_directories(${ARCH_SOURCES_DIR})
list(APPEND UNWINDER_SOURCES
    ${ARCH_SOURCES_DIR}/unwinder.cpp
)

convert_to_absolute_path(UNWINDER_SOURCES ${UNWINDER_SOURCES})

if(CLR_CMAKE_HOST_UNIX)
    add_library_clr(unwinder_wks OBJECT ${UNWINDER_SOURCES})
    add_dependencies(unwinder_wks eventing_headers)
endif(CLR_CMAKE_HOST_UNIX)

add_library_clr(unwinder_dac ${UNWINDER_SOURCES})
add_dependencies(unwinder_dac eventing_headers)
set_target_properties(unwinder_dac PROPERTIES DAC_COMPONENT TRUE)
target_compile_definitions(unwinder_dac PRIVATE FEATURE_NO_HOST)

add_library_clr(unwinder_cdac SHARED ${UNWINDER_SOURCES})
add_dependencies(unwinder_cdac eventing_headers)
target_link_libraries(unwinder_cdac PRIVATE  ${STATIC_MT_CRT_LIB} ${STATIC_MT_VCRT_LIB})
target_compile_definitions(unwinder_cdac PRIVATE FEATURE_NO_HOST FEATURE_CDAC_UNWINDER)
install_clr(TARGETS unwinder_cdac DESTINATIONS cdaclibs COMPONENT debug INSTALL_ALL_ARTIFACTS)

add_library_clr(unwinder_cdac_static ${UNWINDER_SOURCES})
add_dependencies(unwinder_cdac_static eventing_headers)
target_link_libraries(unwinder_cdac_static PRIVATE  ${STATIC_MT_CRT_LIB} ${STATIC_MT_VCRT_LIB})
target_compile_definitions(unwinder_cdac_static PRIVATE FEATURE_NO_HOST FEATURE_CDAC_UNWINDER)
install_clr(TARGETS unwinder_cdac_static DESTINATIONS cdaclibs COMPONENT debug)
