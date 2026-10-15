
include_directories(${INC_PLATFORM_DIR})
add_library(ExceptionInteropNative ${TEST_LIB_TYPE} ExceptionInteropNative.cpp)
target_link_libraries(ExceptionInteropNative PRIVATE platformdefines)