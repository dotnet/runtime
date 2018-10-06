cmake_minimum_required (VERSION 2.6)
project (BSTRTestNative)
include_directories(${INC_PLATFORM_DIR})
set(SOURCES BSTRTestNative.cpp)

# add the executable
add_library (BSTRTestNative SHARED ${SOURCES})

if(WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(WIN32)

target_link_libraries(BSTRTestNative ${LINK_LIBRARIES_ADDITIONAL}) 

# add the install targets
install (TARGETS BSTRTestNative DESTINATION bin)
