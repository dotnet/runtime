project (IjwNativeDll)
include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwNativeDll.cpp)

# add the shared library
add_library (IjwNativeDll SHARED ${SOURCES})
target_link_libraries(IjwNativeDll PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
add_ijw_msbuild_project_properties(IjwNativeDll ijwhost)

# add the install targets
install (TARGETS IjwNativeDll DESTINATION bin)
