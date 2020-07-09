include ("${CLR_INTEROP_TEST_ROOT}/Interop.cmake")
set(SOURCES BStrTestNative.cpp)

# add the executable
add_library (BStrTestNative SHARED ${SOURCES})

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND LINK_LIBRARIES_ADDITIONAL
        OleAut32.lib
    )
endif(CLR_CMAKE_HOST_WIN32)

target_link_libraries(BStrTestNative ${LINK_LIBRARIES_ADDITIONAL})

# add the install targets
install (TARGETS BStrTestNative DESTINATION bin)
