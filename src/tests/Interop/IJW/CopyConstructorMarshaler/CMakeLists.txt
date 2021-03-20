project (CopyConstructorMarshaler)
include("../IJW.cmake")

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwCopyConstructorMarshaler.cpp)

# add the shared library
add_library (IjwCopyConstructorMarshaler SHARED ${SOURCES})
target_link_libraries(IjwCopyConstructorMarshaler ${LINK_LIBRARIES_ADDITIONAL})
set_target_properties(IjwCopyConstructorMarshaler PROPERTIES COMMON_LANGUAGE_RUNTIME "netcore")

# add the install targets
install (TARGETS IjwCopyConstructorMarshaler DESTINATION bin)
