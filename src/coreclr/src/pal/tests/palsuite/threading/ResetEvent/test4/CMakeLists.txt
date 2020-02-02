set(SOURCES
  test4.cpp
)

add_executable(paltest_resetevent_test4
  ${SOURCES}
)

add_dependencies(paltest_resetevent_test4 coreclrpal)

target_link_libraries(paltest_resetevent_test4
  ${COMMON_TEST_LIBRARIES}
)
