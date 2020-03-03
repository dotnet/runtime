set(SOURCES
  CreateFileMappingW.cpp
)

add_executable(paltest_createfilemappingw_test3
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test3 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test3
  ${COMMON_TEST_LIBRARIES}
)
