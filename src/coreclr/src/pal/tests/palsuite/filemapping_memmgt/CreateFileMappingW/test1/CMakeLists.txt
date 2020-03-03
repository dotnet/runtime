set(SOURCES
  CreateFileMappingW.cpp
)

add_executable(paltest_createfilemappingw_test1
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_test1 coreclrpal)

target_link_libraries(paltest_createfilemappingw_test1
  ${COMMON_TEST_LIBRARIES}
)
