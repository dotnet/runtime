set(SOURCES
  createsemaphore.cpp
)

add_executable(paltest_createsemaphorew_releasesemaphore_test3
  ${SOURCES}
)

add_dependencies(paltest_createsemaphorew_releasesemaphore_test3 coreclrpal)

target_link_libraries(paltest_createsemaphorew_releasesemaphore_test3
  ${COMMON_TEST_LIBRARIES}
)
