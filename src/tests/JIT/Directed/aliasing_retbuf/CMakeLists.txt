project(AliasingRetBufNative)

# This test always needs to be optimized to hit the problem.
set(CMAKE_BUILD_TYPE Release)

add_library(AliasingRetBufNative ${TEST_LIB_TYPE} aliasing_retbuf_native.cpp)

