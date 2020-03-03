set(SOURCES
  LoadLibraryA.cpp
)

add_executable(paltest_loadlibrarya_test2
  ${SOURCES}
)

add_dependencies(paltest_loadlibrarya_test2 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test2
  ${COMMON_TEST_LIBRARIES}
)
