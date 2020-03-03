set(SOURCES
  CreateFileMappingW.cpp
)

add_executable(paltest_createfilemappingw_test2
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test2 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test2
  ${COMMON_TEST_LIBRARIES}
)
