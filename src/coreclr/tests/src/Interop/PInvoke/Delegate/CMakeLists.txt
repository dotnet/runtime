
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES
    DelegateTestNative.cpp
)

if (CLR_CMAKE_TARGET_WIN32)
  list(APPEND SOURCES DelegateAsInterfaceTestNative.cpp)
endif()

# add the executable
add_library (DelegateTestNative SHARED ${SOURCES})
target_link_libraries(DelegateTestNative ${LINK_LIBRARIES_ADDITIONAL})
