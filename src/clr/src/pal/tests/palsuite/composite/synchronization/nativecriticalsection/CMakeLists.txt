cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(SOURCES
  mtx_critsect.cpp
  pal_composite_native_cs.cpp
  resultbuffer.cpp
)

add_executable(paltest_synchronization_nativecriticalsection
  ${SOURCES}
)

add_dependencies(paltest_synchronization_nativecriticalsection coreclrpal)

target_link_libraries(paltest_synchronization_nativecriticalsection
  ${COMMON_TEST_LIBRARIES}
)
