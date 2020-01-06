set(SOURCES
  PAL_errno.cpp
)

add_executable(paltest_pal_errno_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_errno_test1 coreclrpal)

target_link_libraries(paltest_pal_errno_test1
  ${COMMON_TEST_LIBRARIES}
)
