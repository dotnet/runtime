cmake_minimum_required (VERSION 2.6)
project (AnsiBStrTestNative)
include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake") 
set(SOURCES AnsiBStrTestNative.cpp)

# add the executable
add_library (AnsiBStrTestNative SHARED ${SOURCES})

if(WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(WIN32)

target_link_libraries(AnsiBStrTestNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS AnsiBStrTestNative DESTINATION bin)
