set(SOURCES
  loadlibraryw.cpp
)

add_executable(paltest_loadlibraryw_test3
  ${SOURCES}
)

add_dependencies(paltest_loadlibraryw_test3 coreclrpal)

target_link_libraries(paltest_loadlibraryw_test3
  ${COMMON_TEST_LIBRARIES}
)
