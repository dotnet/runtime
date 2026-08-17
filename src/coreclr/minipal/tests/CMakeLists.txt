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

    add_executable_clr(coreclrminipal_process_watcher_tests
        EXCLUDE_FROM_ALL
        wait.cpp
        ../Unix/wait.cpp
    )

    target_compile_definitions(coreclrminipal_process_watcher_tests
        PRIVATE
        MINIPAL_WAIT_FORCE_PROCESS_WATCHER=1
        MINIPAL_WAIT_TESTS=1
    )

    if(HAVE_PIPE2)
        target_compile_definitions(coreclrminipal_process_watcher_tests PRIVATE HAVE_PIPE2=1)
    else()
        target_compile_definitions(coreclrminipal_process_watcher_tests PRIVATE HAVE_PIPE2=0)
    endif()

    target_link_libraries(coreclrminipal_process_watcher_tests
        PRIVATE
        minipal
        pthread
    )
endif()
