set(SOURCES
  VirtualProtect.cpp
)

add_executable(paltest_virtualprotect_test6
  ${SOURCES}
)

add_dependencies(paltest_virtualprotect_test6 coreclrpal)

target_link_libraries(paltest_virtualprotect_test6
  ${COMMON_TEST_LIBRARIES}
)
