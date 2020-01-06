set(SOURCES
  event.cpp
  main.cpp
)

add_executable(paltest_event_nonshared
  ${SOURCES}
)

add_dependencies(paltest_event_nonshared coreclrpal)

target_link_libraries(paltest_event_nonshared
  ${COMMON_TEST_LIBRARIES}
  rt
)
