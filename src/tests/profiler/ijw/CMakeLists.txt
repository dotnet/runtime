project (IjwProfileeDll)
include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwProfileeDll.cpp)

# add the shared library
add_library (IjwProfileeDll SHARED ${SOURCES})
target_link_libraries(IjwProfileeDll PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
add_ijw_msbuild_project_properties(IjwProfileeDll ijwhost)

# add the install targets
install (TARGETS IjwProfileeDll DESTINATION bin)
