if(ENABLE_PERFTRACING)

    # TODO: Add support for dynamic components once package/deploy have been resolved.
    if(ENABLE_EVENTPIPE_TEST AND STATIC_COMPONENTS AND (NOT DISABLE_COMPONENTS) AND (NOT DISABLE_LIBS) AND (NOT DISABLE_EXECUTABLES))
        set(EVENTPIPE_TEST_SOURCES "")
        set(EVENTPIPE_TEST_HEADERS "")

        list(APPEND EVENTPIPE_TEST_SOURCES
            ep-buffer-manager-tests.c
            ep-buffer-tests.c
            ep-fake-tests.c
            ep-fastserializer-tests.c
            ep-file-tests.c
            ep-provider-callback-dataqueue-tests.c
            ep-rt-tests.c
            ep-session-tests.c
            ep-setup-tests.c
            ep-teardown-tests.c
            ep-tests.c
            ep-test-runner.c
            ep-test-driver.c
            ep-thread-tests.c
            dn-vector-tests.c
            dn-vector-ptr-tests.c
            dn-fwd-list-tests.c
            dn-list-tests.c
            dn-queue-tests.c
            dn-umap-tests.c
        )

        list(APPEND EVENTPIPE_TEST_HEADERS
            ep-tests.h
            ep-tests-debug.h
        )

        addprefix(EVENTPIPE_TEST_SOURCES ${MONO_EVENTPIPE_TEST_SOURCE_PATH} "${EVENTPIPE_TEST_SOURCES}")
        addprefix(EVENTPIPE_TEST_HEADERS ${MONO_EVENTPIPE_TEST_SOURCE_PATH} "${EVENTPIPE_TEST_HEADERS}")

        set(CMAKE_SKIP_RPATH 1)
        add_executable(ep-test ${EVENTPIPE_TEST_SOURCES} ${EVENTPIPE_TEST_HEADERS})
        target_sources(ep-test PRIVATE "${mono-components-objects}")
        target_link_libraries(ep-test PRIVATE eglib_api monosgen-static ${OS_LIBS} ${LLVM_LIBS} ${Z_LIBS} monoapi)
        install_with_stripped_symbols(ep-test TARGETS bin)
    else(ENABLE_EVENTPIPE_TEST AND STATIC_COMPONENTS AND (NOT DISABLE_COMPONENTS) AND (NOT DISABLE_LIBS) AND (NOT DISABLE_EXECUTABLES))
        message(VERBOSE "Skip building native EventPipe library test runner.")
    endif(ENABLE_EVENTPIPE_TEST AND STATIC_COMPONENTS AND (NOT DISABLE_COMPONENTS) AND (NOT DISABLE_LIBS) AND (NOT DISABLE_EXECUTABLES))

endif(ENABLE_PERFTRACING)
