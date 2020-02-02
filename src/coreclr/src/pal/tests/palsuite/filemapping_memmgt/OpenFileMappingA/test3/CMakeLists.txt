set(SOURCES
  OpenFileMappingA.cpp
)

add_executable(paltest_openfilemappinga_test3
  ${SOURCES}
)

add_dependencies(paltest_openfilemappinga_test3 coreclrpal)

target_link_libraries(paltest_openfilemappinga_test3
  ${COMMON_TEST_LIBRARIES}
)
