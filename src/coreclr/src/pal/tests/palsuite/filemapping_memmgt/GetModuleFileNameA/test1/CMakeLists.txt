set(SOURCES
  GetModuleFileNameA.cpp
)

add_executable(paltest_getmodulefilenamea_test1
  ${SOURCES}
)

add_dependencies(paltest_getmodulefilenamea_test1 coreclrpal)

target_link_libraries(paltest_getmodulefilenamea_test1
  ${COMMON_TEST_LIBRARIES}
)
