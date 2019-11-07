cmake_minimum_required (VERSION 2.6)
project (IjwNativeVarargs)
include("../IJW.cmake")

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwNativeVarargs.cpp)

# add the shared library
add_library (IjwNativeVarargs SHARED ${SOURCES})
target_link_libraries(IjwNativeVarargs ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS IjwNativeVarargs DESTINATION bin)
