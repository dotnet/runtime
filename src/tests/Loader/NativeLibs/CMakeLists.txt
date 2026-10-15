project(FromNativePaths_lib)

set(CMAKE_SHARED_LIBRARY_PREFIX "")

set(SOURCES FromNativePaths_lib.cpp)
add_library(FromNativePaths_lib ${TEST_LIB_TYPE} ${SOURCES})

install(TARGETS FromNativePaths_lib DESTINATION Loader/NativeLibs)
