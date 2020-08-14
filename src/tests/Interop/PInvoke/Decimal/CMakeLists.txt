 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    DecimalTestNative.cpp 
)
add_library (DecimalTestNative SHARED ${SOURCES}) 
target_link_libraries(DecimalTestNative ${LINK_LIBRARIES_ADDITIONAL})
install (TARGETS DecimalTestNative DESTINATION bin) 
