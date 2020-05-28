set(SOURCES
  FILECanonicalizePath.cpp
)

add_executable(paltest_filecanonicalizepath_test1
  ${SOURCES}
)

add_dependencies(paltest_filecanonicalizepath_test1 coreclrpal)

target_link_libraries(paltest_filecanonicalizepath_test1
  ${COMMON_TEST_LIBRARIES}
)
