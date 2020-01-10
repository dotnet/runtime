set(SOURCES
  loadlibrarya.cpp
)

add_executable(paltest_loadlibrarya_test3
  ${SOURCES}
)

add_dependencies(paltest_loadlibrarya_test3 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test3
  ${COMMON_TEST_LIBRARIES}
)
