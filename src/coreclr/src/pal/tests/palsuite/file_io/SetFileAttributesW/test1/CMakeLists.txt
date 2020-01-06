set(SOURCES
  SetFileAttributesW.cpp
)

add_executable(paltest_setfileattributesw_test1
  ${SOURCES}
)

add_dependencies(paltest_setfileattributesw_test1 coreclrpal)

target_link_libraries(paltest_setfileattributesw_test1
  ${COMMON_TEST_LIBRARIES}
)
