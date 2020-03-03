set(SOURCES
  VirtualProtect.cpp
)

add_executable(paltest_virtualprotect_test4
  ${SOURCES}
)

add_dependencies(paltest_virtualprotect_test4 coreclrpal)

target_link_libraries(paltest_virtualprotect_test4
  ${COMMON_TEST_LIBRARIES}
)
