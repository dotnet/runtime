set(SOURCES
  loadlibrarya.cpp
)

add_executable(paltest_loadlibrarya_test5
  ${SOURCES}
)

add_dependencies(paltest_loadlibrarya_test5 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test5
  ${COMMON_TEST_LIBRARIES}
)
