set(SOURCES
  ThreadPriority.cpp
)

add_executable(paltest_threadpriority_test1
  ${SOURCES}
)

add_dependencies(paltest_threadpriority_test1 coreclrpal)

target_link_libraries(paltest_threadpriority_test1
  ${COMMON_TEST_LIBRARIES}
)
