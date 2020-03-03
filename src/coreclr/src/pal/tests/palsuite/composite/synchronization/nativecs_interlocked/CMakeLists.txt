set(SOURCES
  interlocked.cpp
  mtx_critsect.cpp
  pal_composite_native_cs.cpp
  resultbuffer.cpp
)

add_executable(paltest_synchronization_nativecs_interlocked
  ${SOURCES}
)

add_dependencies(paltest_synchronization_nativecs_interlocked coreclrpal)

target_link_libraries(paltest_synchronization_nativecs_interlocked
  ${COMMON_TEST_LIBRARIES}
)
