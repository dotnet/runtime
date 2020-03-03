set(SOURCES
  CreateFileMappingW.cpp
)

add_executable(paltest_createfilemappingw_test6
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test6 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test6
  ${COMMON_TEST_LIBRARIES}
)
