set(SOURCES
  CreateFileMappingW.cpp
)

add_executable(paltest_createfilemappingw_test4
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test4 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test4
  ${COMMON_TEST_LIBRARIES}
)
