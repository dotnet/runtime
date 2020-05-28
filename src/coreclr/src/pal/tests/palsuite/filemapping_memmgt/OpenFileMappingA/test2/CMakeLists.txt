set(SOURCES
  OpenFileMappingA.cpp
)

add_executable(paltest_openfilemappinga_test2
  ${SOURCES}
)

add_dependencies(paltest_openfilemappinga_test2 coreclrpal)

target_link_libraries(paltest_openfilemappinga_test2
  ${COMMON_TEST_LIBRARIES}
)
