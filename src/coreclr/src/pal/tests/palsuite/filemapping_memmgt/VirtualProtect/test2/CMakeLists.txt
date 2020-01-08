set(SOURCES
  VirtualProtect.cpp
)

add_executable(paltest_virtualprotect_test2
  ${SOURCES}
)

add_dependencies(paltest_virtualprotect_test2 coreclrpal)

target_link_libraries(paltest_virtualprotect_test2
  ${COMMON_TEST_LIBRARIES}
)
