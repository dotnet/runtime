set(SOURCES
  VirtualProtect.cpp
)

add_executable(paltest_virtualprotect_test7
  ${SOURCES}
)

add_dependencies(paltest_virtualprotect_test7 coreclrpal)

target_link_libraries(paltest_virtualprotect_test7
  ${COMMON_TEST_LIBRARIES}
)
