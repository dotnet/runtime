set(SOURCES
  createfilemapping.cpp
)

add_executable(paltest_createfilemappingw_test7
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test7 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test7
  ${COMMON_TEST_LIBRARIES}
)
