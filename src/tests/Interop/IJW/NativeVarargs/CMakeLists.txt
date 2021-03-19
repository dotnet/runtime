project (IjwNativeVarargs)
include("../IJW.cmake")

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwNativeVarargs.cpp)

# add the shared library
add_library (IjwNativeVarargs SHARED ${SOURCES})
target_link_libraries(IjwNativeVarargs ${LINK_LIBRARIES_ADDITIONAL})
set_target_properties(IjwNativeVarargs PROPERTIES COMMON_LANGUAGE_RUNTIME "")

# add the install targets
install (TARGETS IjwNativeVarargs DESTINATION bin)
