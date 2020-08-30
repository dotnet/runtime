 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES 
    Vector2_3_4TestNative.cpp 
)
add_library (Vector2_3_4TestNative SHARED ${SOURCES}) 
target_link_libraries(Vector2_3_4TestNative ${LINK_LIBRARIES_ADDITIONAL})
install (TARGETS Vector2_3_4TestNative DESTINATION bin) 
