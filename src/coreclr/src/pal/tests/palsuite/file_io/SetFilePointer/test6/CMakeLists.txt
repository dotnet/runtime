set(SOURCES
  SetFilePointer.cpp
)

add_executable(paltest_setfilepointer_test6
  ${SOURCES}
)

add_dependencies(paltest_setfilepointer_test6 coreclrpal)

target_link_libraries(paltest_setfilepointer_test6
  ${COMMON_TEST_LIBRARIES}
)
