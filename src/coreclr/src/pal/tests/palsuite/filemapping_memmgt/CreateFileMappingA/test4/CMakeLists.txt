set(SOURCES
  CreateFileMapping.cpp
)

add_executable(paltest_createfilemappinga_test4
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test4 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test4
  ${COMMON_TEST_LIBRARIES}
)
