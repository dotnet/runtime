set(SOURCES
  main.cpp
  semaphore.cpp
)

add_executable(paltest_semaphore_nonshared
  ${SOURCES}
)

add_dependencies(paltest_semaphore_nonshared coreclrpal)

target_link_libraries(paltest_semaphore_nonshared
  ${COMMON_TEST_LIBRARIES}
  rt
)
