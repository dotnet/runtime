if(ENABLE_PERFTRACING)

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

	set(eventpipe_test_sources_base "")

	list(APPEND eventpipe_test_sources_base
		${EVENTPIPE_TEST_SOURCES}
		${EVENTPIPE_TEST_HEADERS}
	)

	addprefix(eventpipe_test_sources_base ../eventpipe/test "${eventpipe_test_sources_base}")

	set(eventpipe_test_sources ${eventpipe_test_sources_base})

endif(ENABLE_PERFTRACING)
