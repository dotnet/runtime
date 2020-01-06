set(SOURCES
  SetFileAttributesW.cpp
)

add_executable(paltest_setfileattributesw_test2
  ${SOURCES}
)

add_dependencies(paltest_setfileattributesw_test2 coreclrpal)

target_link_libraries(paltest_setfileattributesw_test2
  ${COMMON_TEST_LIBRARIES}
)
