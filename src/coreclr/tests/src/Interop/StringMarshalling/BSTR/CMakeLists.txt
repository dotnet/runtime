cmake_minimum_required (VERSION 2.6)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES BStrTestNative.cpp)

# add the executable
add_library (BStrTestNative SHARED ${SOURCES})

if(WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(WIN32)

target_link_libraries(BStrTestNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS BStrTestNative DESTINATION bin)
