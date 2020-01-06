set(SOURCES
  CreateFileMapping.cpp
)

add_executable(paltest_createfilemappinga_test6
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test6 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test6
  ${COMMON_TEST_LIBRARIES}
)
