set(SOURCES
  criticalsection.cpp
  mainWrapper.cpp
)

add_executable(paltest_synchronization_criticalsection
  ${SOURCES}
)

add_dependencies(paltest_synchronization_criticalsection coreclrpal)

target_link_libraries(paltest_synchronization_criticalsection
  ${COMMON_TEST_LIBRARIES}
)
