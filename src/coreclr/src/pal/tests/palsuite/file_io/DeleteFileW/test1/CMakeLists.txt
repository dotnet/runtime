set(SOURCES
  DeleteFileW.cpp
)

add_executable(paltest_deletefilew_test1
  ${SOURCES}
)

add_dependencies(paltest_deletefilew_test1 coreclrpal)

target_link_libraries(paltest_deletefilew_test1
  ${COMMON_TEST_LIBRARIES}
)
