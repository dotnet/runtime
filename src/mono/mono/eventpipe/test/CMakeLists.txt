if(ENABLE_PERFTRACING)

    if(ENABLE_EVENTPIPE_TEST AND (NOT DISABLE_LIBS) AND (NOT DISABLE_EXECUTABLES))
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
        )

        list(APPEND EVENTPIPE_TEST_HEADERS
            ep-tests.h
            ep-tests-debug.h
        )

        include_directories(
            ${PROJECT_SOURCE_DIR}/../../native/
            ${PROJECT_SOURCE_DIR}/..
            ${PROJECT_SOURCE_DIR}
            ${PROJECT_SOURCE_DIR}/eglib
            ${PROJECT_BINARY_DIR}/..
            ${PROJECT_BINARY_DIR}/eglib
        )

        if(HAVE_SYS_ICU)
            if(ICU_LIBDIR)
                set(ICU_LDFLAGS "-L${ICU_LIBDIR}")
            endif()
        endif()

        if(HOST_DARWIN)
            set(OS_LIBS "-framework CoreFoundation" "-framework Foundation")
        elseif(HOST_IOS)
            set(OS_LIBS "-framework CoreFoundation" "-lobjc" "-lc++")
        elseif(HOST_ANDROID)
            set(OS_LIBS m dl log)
        elseif(HOST_LINUX)
            set(OS_LIBS pthread m dl)
        elseif(HOST_WIN32)
            set(OS_LIBS bcrypt.lib ws2_32.lib version.lib winmm.lib)
        endif()

        set(CMAKE_SKIP_RPATH 1)
        add_executable(ep-test ${EVENTPIPE_TEST_SOURCES} ${EVENTPIPE_TEST_HEADERS})
        target_link_libraries(ep-test monosgen-static ${OS_LIBS} ${ICONV_LIB} ${LLVM_LIBS} ${ICU_LIBS})
        if(ICU_LDFLAGS)
            set_target_properties(ep-test PROPERTIES LINK_FLAGS ${ICU_LDFLAGS})
        endif()
        install(TARGETS ep-test RUNTIME)
    else(ENABLE_EVENTPIPE_TEST AND (NOT DISABLE_LIBS) AND (NOT DISABLE_EXECUTABLES))
        message(VERBOSE "Skip building native EventPipe library test runner.")
    endif(ENABLE_EVENTPIPE_TEST AND (NOT DISABLE_LIBS) AND (NOT DISABLE_EXECUTABLES))

endif(ENABLE_PERFTRACING)
