set(SOURCES
  SetFileAttributesA.cpp
)

add_executable(paltest_setfileattributesa_test3
  ${SOURCES}
)

add_dependencies(paltest_setfileattributesa_test3 coreclrpal)

target_link_libraries(paltest_setfileattributesa_test3
  ${COMMON_TEST_LIBRARIES}
)
