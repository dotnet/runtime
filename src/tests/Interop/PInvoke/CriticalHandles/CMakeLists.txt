 
project (CriticalHandlesNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    CriticalHandlesNative.cpp 
) 
# add the executable 
add_library (CriticalHandlesNative SHARED ${SOURCES}) 
target_link_libraries(CriticalHandlesNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS CriticalHandlesNative DESTINATION bin) 
