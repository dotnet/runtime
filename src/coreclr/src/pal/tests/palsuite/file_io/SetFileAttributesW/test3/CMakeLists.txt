set(SOURCES
  SetFileAttributesW.cpp
)

add_executable(paltest_setfileattributesw_test3
  ${SOURCES}
)

add_dependencies(paltest_setfileattributesw_test3 coreclrpal)

target_link_libraries(paltest_setfileattributesw_test3
  ${COMMON_TEST_LIBRARIES}
)
