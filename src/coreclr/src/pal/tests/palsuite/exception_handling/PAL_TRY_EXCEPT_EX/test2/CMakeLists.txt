set(SOURCES
  PAL_TRY_EXCEPT_EX.cpp
)

add_executable(paltest_pal_try_except_ex_test2
  ${SOURCES}
)

add_dependencies(paltest_pal_try_except_ex_test2 coreclrpal)

target_link_libraries(paltest_pal_try_except_ex_test2
  ${COMMON_TEST_LIBRARIES}
)
