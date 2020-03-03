set(SOURCES
  CreateMutexA.cpp
)

add_executable(paltest_createmutexa_releasemutex_test2
  ${SOURCES}
)

add_dependencies(paltest_createmutexa_releasemutex_test2 coreclrpal)

target_link_libraries(paltest_createmutexa_releasemutex_test2
  ${COMMON_TEST_LIBRARIES}
)
