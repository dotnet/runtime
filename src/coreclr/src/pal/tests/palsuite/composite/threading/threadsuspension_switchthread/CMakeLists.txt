set(SOURCES
  mainWrapper.cpp
  threadsuspension.cpp
)

add_executable(paltest_threading_threadsuspension_switchthread
  ${SOURCES}
)

add_dependencies(paltest_threading_threadsuspension_switchthread coreclrpal)

target_link_libraries(paltest_threading_threadsuspension_switchthread
  ${COMMON_TEST_LIBRARIES}
)
