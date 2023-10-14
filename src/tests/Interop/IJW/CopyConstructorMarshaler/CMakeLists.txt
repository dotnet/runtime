project (CopyConstructorMarshaler)
include(${CLR_ENG_NATIVE_DIR}/ijw/IJW.cmake)

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwCopyConstructorMarshaler.cpp)

# add the shared library
add_library (IjwCopyConstructorMarshaler SHARED ${SOURCES})
target_link_libraries(IjwCopyConstructorMarshaler PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
add_ijw_msbuild_project_properties(IjwCopyConstructorMarshaler ijwhost)

# add the install targets
install (TARGETS IjwCopyConstructorMarshaler DESTINATION bin)
