cmake_minimum_required (VERSION 2.6) 
project (NativeDateTime) 
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
include_directories(${INC_PLATFORM_DIR}) 
set(SOURCES 
    NativeDateTime.cpp 
) 
if(WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(WIN32)
# add the executable 
add_library (NativeDateTime SHARED ${SOURCES}) 
target_link_libraries(NativeDateTime ${LINK_LIBRARIES_ADDITIONAL}) 
# add the install targets 
install (TARGETS NativeDateTime DESTINATION bin) 
