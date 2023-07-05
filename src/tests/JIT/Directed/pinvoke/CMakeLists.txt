project(PInvokeExampleNative)

add_library(PInvokeExampleNative SHARED pinvokeexamplenative.cpp)
SET_TARGET_PROPERTIES(PInvokeExampleNative PROPERTIES COMPILE_FLAGS "-c")
