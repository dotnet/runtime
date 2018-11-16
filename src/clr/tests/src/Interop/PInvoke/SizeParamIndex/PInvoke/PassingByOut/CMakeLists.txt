cmake_minimum_required (VERSION 2.6) 
project (PInvokePassingByOutNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
include_directories("..")
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    PInvokePassingByOutNative.cpp 
)
# add the executable 
add_library (PInvokePassingByOutNative SHARED ${SOURCES}) 
target_link_libraries(PInvokePassingByOutNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS PInvokePassingByOutNative DESTINATION bin) 
