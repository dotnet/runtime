project (NativeLibrary)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the shared library
add_library (NativeLibrary SHARED NativeLibrary.cpp)

if (CLR_CMAKE_TARGET_UNIX)
  target_link_libraries(NativeLibrary ${LINK_LIBRARIES_ADDITIONAL})
endif()

# add the shared library
add_library (GlobalLoadHelper SHARED GlobalLoadHelper.cpp)
target_link_libraries(GlobalLoadHelper ${LINK_LIBRARIES_ADDITIONAL})

add_library (GloballyLoadedNativeLibrary SHARED GloballyLoadedNativeLibrary.cpp)
target_link_libraries(GloballyLoadedNativeLibrary ${LINK_LIBRARIES_ADDITIONAL})
