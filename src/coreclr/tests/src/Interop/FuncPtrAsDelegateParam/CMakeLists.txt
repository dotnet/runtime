cmake_minimum_required (VERSION 2.6)
project (FuncPtrAsDelegateParamNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES FuncPtrAsDelegateParamNative.cpp )

# add the executable
add_library (FuncPtrAsDelegateParamNative SHARED ${SOURCES})
target_link_libraries(FuncPtrAsDelegateParamNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS FuncPtrAsDelegateParamNative DESTINATION bin)
