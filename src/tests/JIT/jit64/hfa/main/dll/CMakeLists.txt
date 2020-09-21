project(hfa_interop)

set(CMAKE_SHARED_LIBRARY_PREFIX "")


add_library(hfa_simple_f32_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_simple_f32_native_cpp PROPERTIES COMPILE_FLAGS "-c -DSIMPLE_HFA -DFLOAT32") 

add_library(hfa_simple_f64_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_simple_f64_native_cpp PROPERTIES COMPILE_FLAGS "-c -DSIMPLE_HFA -DFLOAT64") 

add_library(hfa_nested_f32_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_nested_f32_native_cpp PROPERTIES COMPILE_FLAGS "-c -DNESTED_HFA -DFLOAT32") 

add_library(hfa_nested_f64_native_cpp SHARED hfa_native.cpp)
SET_TARGET_PROPERTIES(hfa_nested_f64_native_cpp PROPERTIES COMPILE_FLAGS "-c -DNESTED_HFA -DFLOAT64") 


# add the install targets (this "installs" the native file on Windows systems)
install(TARGETS hfa_simple_f32_native_cpp DESTINATION bin)
install(TARGETS hfa_simple_f64_native_cpp DESTINATION bin)
install(TARGETS hfa_nested_f32_native_cpp DESTINATION bin)
install(TARGETS hfa_nested_f64_native_cpp DESTINATION bin)

# This "installs" the native file on System V systems
set_target_properties(hfa_simple_f32_native_cpp PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/hfa_simple_f32_native_cpp)
set_target_properties(hfa_simple_f64_native_cpp PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/hfa_simple_f64_native_cpp)
set_target_properties(hfa_nested_f32_native_cpp PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/hfa_nested_f32_native_cpp)
set_target_properties(hfa_nested_f64_native_cpp PROPERTIES LIBRARY_OUTPUT_DIRECTORY ${PROJECT_BINARY_DIR}/hfa_nested_f64_native_cpp)
