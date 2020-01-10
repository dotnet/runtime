set(SOURCES
  PAL_TRY_LEAVE_FINALLY.cpp
)

add_executable(paltest_pal_try_leave_finally_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_try_leave_finally_test1 coreclrpal)

target_link_libraries(paltest_pal_try_leave_finally_test1
  ${COMMON_TEST_LIBRARIES}
)
