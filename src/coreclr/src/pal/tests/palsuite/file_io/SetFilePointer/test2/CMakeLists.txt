set(SOURCES
  SetFilePointer.cpp
)

add_executable(paltest_setfilepointer_test2
  ${SOURCES}
)

add_dependencies(paltest_setfilepointer_test2 coreclrpal)

target_link_libraries(paltest_setfilepointer_test2
  ${COMMON_TEST_LIBRARIES}
)
