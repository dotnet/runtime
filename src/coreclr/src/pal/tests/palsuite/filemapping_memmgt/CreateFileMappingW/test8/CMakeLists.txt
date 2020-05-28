set(SOURCES
  createfilemapping.cpp
)

add_executable(paltest_createfilemappingw_test8
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test8 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test8
  ${COMMON_TEST_LIBRARIES}
)
