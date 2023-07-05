project(AliasingRetBufNative)

# This test always needs to be optimized to hit the problem.
set(CMAKE_BUILD_TYPE Release)

add_library(AliasingRetBufNative SHARED aliasing_retbuf_native.cpp)

