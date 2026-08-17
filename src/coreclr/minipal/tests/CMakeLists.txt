add_executable_clr(coreclrminipal_wait_tests
    EXCLUDE_FROM_ALL
    wait.cpp
)

target_link_libraries(coreclrminipal_wait_tests
    PRIVATE
    coreclrminipal
)

if(CLR_CMAKE_HOST_UNIX)
    target_link_libraries(coreclrminipal_wait_tests PRIVATE pthread)
endif()
