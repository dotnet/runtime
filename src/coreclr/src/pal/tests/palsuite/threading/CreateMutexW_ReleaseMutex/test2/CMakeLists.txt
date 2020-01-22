set(SOURCES
  CreateMutexW.cpp
)

add_executable(paltest_createmutexw_releasemutex_test2
  ${SOURCES}
)

add_dependencies(paltest_createmutexw_releasemutex_test2 coreclrpal)

target_link_libraries(paltest_createmutexw_releasemutex_test2
  ${COMMON_TEST_LIBRARIES}
)
