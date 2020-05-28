set(SOURCES
  FindNextFileW.cpp
)

add_executable(paltest_findnextfilew_test1
  ${SOURCES}
)

add_dependencies(paltest_findnextfilew_test1 coreclrpal)

target_link_libraries(paltest_findnextfilew_test1
  ${COMMON_TEST_LIBRARIES}
)
