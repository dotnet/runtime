set(SOURCES
  SetFilePointer.cpp
)

add_executable(paltest_setfilepointer_test1
  ${SOURCES}
)

add_dependencies(paltest_setfilepointer_test1 coreclrpal)

target_link_libraries(paltest_setfilepointer_test1
  ${COMMON_TEST_LIBRARIES}
)
