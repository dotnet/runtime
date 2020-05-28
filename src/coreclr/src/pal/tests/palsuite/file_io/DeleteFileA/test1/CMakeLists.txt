set(SOURCES
  DeleteFileA.cpp
)

add_executable(paltest_deletefilea_test1
  ${SOURCES}
)

add_dependencies(paltest_deletefilea_test1 coreclrpal)

target_link_libraries(paltest_deletefilea_test1
  ${COMMON_TEST_LIBRARIES}
)
