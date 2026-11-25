include_directories(${INC_PLATFORM_DIR})
add_library(CrossplatVirtualAlloc ${TEST_LIB_TYPE} CrossplatVirtualAlloc.cpp)
target_link_libraries(CrossplatVirtualAlloc PRIVATE platformdefines)
