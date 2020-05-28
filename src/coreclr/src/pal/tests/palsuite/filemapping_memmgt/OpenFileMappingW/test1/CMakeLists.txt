set(SOURCES
  OpenFileMappingW.cpp
)

add_executable(paltest_openfilemappingw_test1
  ${SOURCES}
)

add_dependencies(paltest_openfilemappingw_test1 coreclrpal)

target_link_libraries(paltest_openfilemappingw_test1
  ${COMMON_TEST_LIBRARIES}
)
