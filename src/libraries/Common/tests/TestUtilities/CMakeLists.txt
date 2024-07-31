include_directories(${INC_PLATFORM_DIR})
add_library(XplatVirtualAlloc SHARED XplatVirtualAlloc.cpp)
target_link_libraries(XplatVirtualAlloc PRIVATE platformdefines)
