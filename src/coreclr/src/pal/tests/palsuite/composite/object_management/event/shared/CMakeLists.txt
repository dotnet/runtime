set(SOURCES
  event.cpp
  main.cpp
)

add_executable(paltest_event_shared
  ${SOURCES}
)

add_dependencies(paltest_event_shared coreclrpal)

target_link_libraries(paltest_event_shared
  ${COMMON_TEST_LIBRARIES}
  rt
)
