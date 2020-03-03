set(SOURCES
  CreateSemaphore.cpp
)

add_executable(paltest_createsemaphorew_releasesemaphore_test2
  ${SOURCES}
)

add_dependencies(paltest_createsemaphorew_releasesemaphore_test2 coreclrpal)

target_link_libraries(paltest_createsemaphorew_releasesemaphore_test2
  ${COMMON_TEST_LIBRARIES}
)
