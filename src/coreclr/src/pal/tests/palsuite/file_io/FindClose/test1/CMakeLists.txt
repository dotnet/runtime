set(SOURCES
  FindClose.cpp
)

add_executable(paltest_findclose_test1
  ${SOURCES}
)

add_dependencies(paltest_findclose_test1 coreclrpal)

target_link_libraries(paltest_findclose_test1
  ${COMMON_TEST_LIBRARIES}
)
