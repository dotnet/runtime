set(SOURCES
  OpenFileMappingW.cpp
)

add_executable(paltest_openfilemappingw_test2
  ${SOURCES}
)

add_dependencies(paltest_openfilemappingw_test2 coreclrpal)

target_link_libraries(paltest_openfilemappingw_test2
  ${COMMON_TEST_LIBRARIES}
)
