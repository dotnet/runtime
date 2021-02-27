cmake_minimum_required(VERSION 3.16)
set(CMAKE_OBJC_STANDARD 11)
set(CMAKE_OBJC_STANDARD_REQUIRED TRUE)

enable_language(OBJC)

project (NativeBridgeTests)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES NativeBridgeTests.cpp)

# add the shared library
add_library (NativeBridgeTests SHARED ${SOURCES})
target_link_libraries(NativeBridgeTests ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS NativeBridgeTests DESTINATION bin)
