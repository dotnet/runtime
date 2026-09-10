project(PInvokeExampleNative)

add_library(PInvokeExampleNative ${TEST_LIB_TYPE} pinvokeexamplenative.cpp)
SET_TARGET_PROPERTIES(PInvokeExampleNative PROPERTIES COMPILE_FLAGS "-c")
