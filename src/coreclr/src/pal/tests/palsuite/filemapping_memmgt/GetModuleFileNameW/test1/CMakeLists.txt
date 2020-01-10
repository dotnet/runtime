set(SOURCES
  GetModuleFileNameW.cpp
)

add_executable(paltest_getmodulefilenamew_test1
  ${SOURCES}
)

add_dependencies(paltest_getmodulefilenamew_test1 coreclrpal)

target_link_libraries(paltest_getmodulefilenamew_test1
  ${COMMON_TEST_LIBRARIES}
)
