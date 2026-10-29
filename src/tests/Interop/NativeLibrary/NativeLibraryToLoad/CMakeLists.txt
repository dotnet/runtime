project (NativeLibrary)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")

# add the shared library
add_library (NativeLibrary ${TEST_LIB_TYPE} NativeLibrary.cpp)

if (CLR_CMAKE_TARGET_UNIX)
  target_link_libraries(NativeLibrary PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
endif()

# add the shared library
add_library (GlobalLoadHelper ${TEST_LIB_TYPE} GlobalLoadHelper.cpp)
target_link_libraries(GlobalLoadHelper PRIVATE ${LINK_LIBRARIES_ADDITIONAL})

add_library (GloballyLoadedNativeLibrary ${TEST_LIB_TYPE} GloballyLoadedNativeLibrary.cpp)
target_link_libraries(GloballyLoadedNativeLibrary PRIVATE ${LINK_LIBRARIES_ADDITIONAL})
