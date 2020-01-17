set(SOURCES
  OpenFileMappingA.cpp
)

add_executable(paltest_openfilemappinga_test1
  ${SOURCES}
)

add_dependencies(paltest_openfilemappinga_test1 coreclrpal)

target_link_libraries(paltest_openfilemappinga_test1
  ${COMMON_TEST_LIBRARIES}
)
