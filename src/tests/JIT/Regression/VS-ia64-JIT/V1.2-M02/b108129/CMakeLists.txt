project(b108129_test2)
include_directories(${INC_PLATFORM_DIR})

add_library(test2 SHARED test2.cpp)
SET_TARGET_PROPERTIES(test2 PROPERTIES COMPILE_FLAGS "-c") 
