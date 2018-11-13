cmake_minimum_required (VERSION 2.6) 
project (AsAnyNative) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    AsAnyNative.cpp 
) 
if(WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(WIN32)
# add the executable 
add_library (AsAnyNative SHARED ${SOURCES}) 
target_link_libraries(AsAnyNative ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS AsAnyNative DESTINATION bin) 
