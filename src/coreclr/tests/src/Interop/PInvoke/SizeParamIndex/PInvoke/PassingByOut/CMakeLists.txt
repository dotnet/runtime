project (PInvokePassingByOutNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories("..") 
set(SOURCES 
    PInvokePassingByOutNative.cpp 
)
# add the executable 
add_library (PInvokePassingByOutNative SHARED ${SOURCES}) 
target_link_libraries(PInvokePassingByOutNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS PInvokePassingByOutNative DESTINATION bin) 
