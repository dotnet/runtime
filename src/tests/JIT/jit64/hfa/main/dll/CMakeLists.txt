project(hfa_interop)


add_library(hfa_simple_f32_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_simple_f32_native_cpp PROPERTIES COMPILE_FLAGS "-c -DSIMPLE_HFA -DFLOAT32") 

add_library(hfa_simple_f64_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_simple_f64_native_cpp PROPERTIES COMPILE_FLAGS "-c -DSIMPLE_HFA -DFLOAT64") 

add_library(hfa_nested_f32_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_nested_f32_native_cpp PROPERTIES COMPILE_FLAGS "-c -DNESTED_HFA -DFLOAT32") 

add_library(hfa_nested_f64_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_nested_f64_native_cpp PROPERTIES COMPILE_FLAGS "-c -DNESTED_HFA -DFLOAT64") 
