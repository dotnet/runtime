set(SOURCES
  createfilemapping.cpp
)

add_executable(paltest_createfilemappinga_test8
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test8 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test8
  ${COMMON_TEST_LIBRARIES}
)
