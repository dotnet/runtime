include_directories(${INC_PLATFORM_DIR})

# This test always needs to be optimized to hit the problem.
set(CMAKE_BUILD_TYPE Release)

add_library(Runtime101046Native SHARED Runtime_101046.cpp)
target_link_libraries(Runtime101046Native PRIVATE platformdefines)
