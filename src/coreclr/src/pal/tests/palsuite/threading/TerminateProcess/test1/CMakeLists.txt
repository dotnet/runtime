set(SOURCES
  TerminateProcess.cpp
)

add_executable(paltest_terminateprocess_test1
  ${SOURCES}
)

add_dependencies(paltest_terminateprocess_test1 coreclrpal)

target_link_libraries(paltest_terminateprocess_test1
  ${COMMON_TEST_LIBRARIES}
)
