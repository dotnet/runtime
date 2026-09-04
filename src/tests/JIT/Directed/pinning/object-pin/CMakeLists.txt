project(object_pin_mirror)

add_library(mirror ${TEST_LIB_TYPE} mirror.cpp)
SET_TARGET_PROPERTIES(mirror PROPERTIES COMPILE_FLAGS "-c") 
