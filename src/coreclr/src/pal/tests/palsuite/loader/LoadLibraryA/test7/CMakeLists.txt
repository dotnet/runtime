set(SOURCES
  LoadLibraryA.cpp
)

add_executable(paltest_loadlibrarya_test7
  ${SOURCES}
)

add_dependencies(paltest_loadlibrarya_test7 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test7
  ${COMMON_TEST_LIBRARIES}
)
