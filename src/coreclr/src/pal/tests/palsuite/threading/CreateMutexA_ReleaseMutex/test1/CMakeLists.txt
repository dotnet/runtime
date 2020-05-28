set(SOURCES
  CreateMutexA.cpp
)

add_executable(paltest_createmutexa_releasemutex_test1
  ${SOURCES}
)

add_dependencies(paltest_createmutexa_releasemutex_test1 coreclrpal)

target_link_libraries(paltest_createmutexa_releasemutex_test1
  ${COMMON_TEST_LIBRARIES}
)
