project (RPIP_ByRefNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories("..") 
set(SOURCES 
    ReversePInvokePassingByRefNative.cpp 
) 
# Additional files to reference: 
# add the executable 
add_library (RPIP_ByRefNative SHARED ${SOURCES}) 
set_property (TARGET RPIP_ByRefNative PROPERTY OUTPUT_NAME ReversePInvokePassingByRefNative)
target_link_libraries(RPIP_ByRefNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS RPIP_ByRefNative DESTINATION bin) 
