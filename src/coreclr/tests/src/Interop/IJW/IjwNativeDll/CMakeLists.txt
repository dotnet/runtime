cmake_minimum_required (VERSION 2.6)
project (IjwNativeDll)
include("../IJW.cmake")

include_directories( ${INC_PLATFORM_DIR} )
set(SOURCES IjwNativeDll.cpp)

# add the shared library
add_library (IjwNativeDll SHARED ${SOURCES})
target_link_libraries(IjwNativeDll ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS IjwNativeDll DESTINATION bin)
