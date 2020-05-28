set(SOURCES
  __iscsym.cpp
)

add_executable(paltest_iscsym_test1
  ${SOURCES}
)

add_dependencies(paltest_iscsym_test1 coreclrpal)

target_link_libraries(paltest_iscsym_test1
  ${COMMON_TEST_LIBRARIES}
)
