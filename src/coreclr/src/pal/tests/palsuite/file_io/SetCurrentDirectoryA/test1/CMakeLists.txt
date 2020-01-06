set(SOURCES
  SetCurrentDirectoryA.cpp
)

add_executable(paltest_setcurrentdirectorya_test1
  ${SOURCES}
)

add_dependencies(paltest_setcurrentdirectorya_test1 coreclrpal)

target_link_libraries(paltest_setcurrentdirectorya_test1
  ${COMMON_TEST_LIBRARIES}
)
