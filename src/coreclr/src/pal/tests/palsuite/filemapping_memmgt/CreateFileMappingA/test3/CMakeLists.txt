set(SOURCES
  CreateFileMapping.cpp
)

add_executable(paltest_createfilemappinga_test3
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test3 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test3
  ${COMMON_TEST_LIBRARIES}
)
