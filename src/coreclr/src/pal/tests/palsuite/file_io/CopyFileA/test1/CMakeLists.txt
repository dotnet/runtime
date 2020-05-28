set(SOURCES
  CopyFileA.cpp
)

add_executable(paltest_copyfilea_test1
  ${SOURCES}
)

add_dependencies(paltest_copyfilea_test1 coreclrpal)

target_link_libraries(paltest_copyfilea_test1
  ${COMMON_TEST_LIBRARIES}
)
