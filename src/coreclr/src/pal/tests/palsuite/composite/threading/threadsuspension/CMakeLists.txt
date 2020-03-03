set(SOURCES
  mainWrapper.cpp
  threadsuspension.cpp
)

add_executable(paltest_threading_threadsuspension
  ${SOURCES}
)

add_dependencies(paltest_threading_threadsuspension coreclrpal)

target_link_libraries(paltest_threading_threadsuspension
  ${COMMON_TEST_LIBRARIES}
)
