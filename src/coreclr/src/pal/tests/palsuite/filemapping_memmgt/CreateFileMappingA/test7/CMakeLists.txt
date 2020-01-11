set(SOURCES
  createfilemapping.cpp
)

add_executable(paltest_createfilemappinga_test7
  ${SOURCES}
)

add_dependencies(paltest_createfilemappinga_test7 coreclrpal)

target_link_libraries(paltest_createfilemappinga_test7
  ${COMMON_TEST_LIBRARIES}
)
