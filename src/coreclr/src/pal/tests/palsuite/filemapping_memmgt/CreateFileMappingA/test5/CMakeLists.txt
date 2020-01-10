set(SOURCES
  CreateFileMapping.cpp
)

add_executable(paltest_createfilemappinga_test5
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test5 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test5
  ${COMMON_TEST_LIBRARIES}
)
