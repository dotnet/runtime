set(SOURCES
  CreateFileMapping.cpp
)

add_executable(paltest_createfilemappinga_test1
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test1 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test1
  ${COMMON_TEST_LIBRARIES}
)
