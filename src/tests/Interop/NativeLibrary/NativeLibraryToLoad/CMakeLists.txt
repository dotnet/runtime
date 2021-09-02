project (NativeLibrary)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES )

# add the shared library
add_library (NativeLibrary SHARED NativeLibrary.cpp)

if (CLR_CMAKE_TARGET_UNIX)
  target_link_libraries(NativeLibrary ${LINK_LIBRARIES_ADDITIONAL} dl)
endif()

# add the shared library
add_library (GloballyLoadedNativeLibrary SHARED GloballyLoadedNativeLibrary.cpp)
target_link_libraries(GloballyLoadedNativeLibrary ${LINK_LIBRARIES_ADDITIONAL})
