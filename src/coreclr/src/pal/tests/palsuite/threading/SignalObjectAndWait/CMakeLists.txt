set(SOURCES
  SignalObjectAndWaitTest.cpp
)

add_executable(paltest_signalobjectandwaittest
  ${SOURCES}
)

add_dependencies(paltest_signalobjectandwaittest coreclrpal)

target_link_libraries(paltest_signalobjectandwaittest
  ${COMMON_TEST_LIBRARIES}
)
