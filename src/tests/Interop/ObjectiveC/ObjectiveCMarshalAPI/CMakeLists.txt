cmake_minimum_required(VERSION 3.16)
set(CMAKE_OBJC_STANDARD 11)
set(CMAKE_OBJC_STANDARD_REQUIRED TRUE)

enable_language(OBJC)

project (NativeObjCMarshalTests)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES NativeObjCMarshalTests.cpp)

# add the shared library
add_library (NativeObjCMarshalTests SHARED ${SOURCES})
target_link_libraries(NativeObjCMarshalTests ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS NativeObjCMarshalTests DESTINATION bin)
