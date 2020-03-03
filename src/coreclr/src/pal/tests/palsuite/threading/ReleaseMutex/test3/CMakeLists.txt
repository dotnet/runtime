set(SOURCES
  ReleaseMutex.cpp
)

add_executable(paltest_releasemutex_test3
  ${SOURCES}
)

add_dependencies(paltest_releasemutex_test3 coreclrpal)

target_link_libraries(paltest_releasemutex_test3
  ${COMMON_TEST_LIBRARIES}
)
