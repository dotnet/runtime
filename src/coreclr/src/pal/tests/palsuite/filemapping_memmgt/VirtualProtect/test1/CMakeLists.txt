set(SOURCES
  VirtualProtect.cpp
)

add_executable(paltest_virtualprotect_test1
  ${SOURCES}
)

add_dependencies(paltest_virtualprotect_test1 coreclrpal)

target_link_libraries(paltest_virtualprotect_test1
  ${COMMON_TEST_LIBRARIES}
)
