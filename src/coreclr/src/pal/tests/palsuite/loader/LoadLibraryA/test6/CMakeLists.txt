set(TESTSOURCES
  loadlibrarya.cpp
)

add_executable(paltest_loadlibrarya_test6
  ${TESTSOURCES}
)

add_dependencies(paltest_loadlibrarya_test6 coreclrpal)

target_link_libraries(paltest_loadlibrarya_test6
  ${COMMON_TEST_LIBRARIES}
)


set(HELPERSOURCES
  dlltest.cpp
)

add_executable(paltest_loadlibrarya_test6_dlltest
  ${HELPERSOURCES}
)

add_dependencies(paltest_loadlibrarya_test6_dlltest coreclrpal)

target_link_libraries(paltest_loadlibrarya_test6_dlltest
  ${COMMON_TEST_LIBRARIES}
)
