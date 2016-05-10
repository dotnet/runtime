cmake_minimum_required (VERSION 2.6)
project (UTF8TestNative)
set(SOURCES UTF8TestNative.cpp )

# add the executable
add_library (UTF8TestNative SHARED ${SOURCES})

# add the install targets
install (TARGETS UTF8TestNative DESTINATION bin)