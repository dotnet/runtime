set(SOURCES
  createfilemapping.cpp
)

add_executable(paltest_createfilemappingw_test9
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test9 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test9
  ${COMMON_TEST_LIBRARIES}
)
