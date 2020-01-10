set(SOURCES
  dlltest.cpp
  test1.cpp
)

add_executable(paltest_freelibraryandexitthread_test1
  ${SOURCES}
)

add_dependencies(paltest_freelibraryandexitthread_test1 coreclrpal)

target_link_libraries(paltest_freelibraryandexitthread_test1
  ${COMMON_TEST_LIBRARIES}
)
