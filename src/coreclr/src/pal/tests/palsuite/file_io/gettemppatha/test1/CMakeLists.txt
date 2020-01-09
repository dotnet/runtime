set(SOURCES
  gettemppatha.cpp
)

add_executable(paltest_gettemppatha_test1
  ${SOURCES}
)

add_dependencies(paltest_gettemppatha_test1 coreclrpal)

target_link_libraries(paltest_gettemppatha_test1
  ${COMMON_TEST_LIBRARIES}
)
