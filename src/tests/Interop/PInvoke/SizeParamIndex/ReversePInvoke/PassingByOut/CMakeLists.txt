project (RPIP_ByOutNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories("..") 
set(SOURCES 
    ReversePInvokePassingByOutNative.cpp 
) 
# Additional files to reference: 
# add the executable 
add_library (RPIP_ByOutNative SHARED ${SOURCES}) 
set_property (TARGET RPIP_ByOutNative PROPERTY OUTPUT_NAME ReversePInvokePassingByOutNative)
target_link_libraries(RPIP_ByOutNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS RPIP_ByOutNative DESTINATION bin) 
