set(SOURCES
  GetModuleFileNameW.cpp
)

add_executable(paltest_getmodulefilenamew_test2
  ${SOURCES}
)

add_dependencies(paltest_getmodulefilenamew_test2 coreclrpal)

target_link_libraries(paltest_getmodulefilenamew_test2
  ${COMMON_TEST_LIBRARIES}
)
