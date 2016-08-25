cmake_minimum_required (VERSION 2.6)
project (UTF8TestNative)
set(SOURCES UTF8TestNative.cpp )

# add the executable
add_library (UTF8TestNative SHARED ${SOURCES})
target_link_libraries(UTF8TestNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS UTF8TestNative DESTINATION bin)