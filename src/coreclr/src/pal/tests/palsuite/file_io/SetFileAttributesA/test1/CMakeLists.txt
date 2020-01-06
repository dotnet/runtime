set(SOURCES
  SetFileAttributesA.cpp
)

add_executable(paltest_setfileattributesa_test1
  ${SOURCES}
)

add_dependencies(paltest_setfileattributesa_test1 coreclrpal)

target_link_libraries(paltest_setfileattributesa_test1
  ${COMMON_TEST_LIBRARIES}
)
