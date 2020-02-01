set(SOURCES
  loadlibraryw.cpp
)

add_executable(paltest_loadlibraryw_test5
  ${SOURCES}
)

add_dependencies(paltest_loadlibraryw_test5 coreclrpal)

target_link_libraries(paltest_loadlibraryw_test5
  ${COMMON_TEST_LIBRARIES}
)
