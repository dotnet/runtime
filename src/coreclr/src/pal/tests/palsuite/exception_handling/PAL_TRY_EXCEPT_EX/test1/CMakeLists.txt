set(SOURCES
  PAL_TRY_EXCEPT_EX.cpp
)

add_executable(paltest_pal_try_except_ex_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_try_except_ex_test1 coreclrpal)

target_link_libraries(paltest_pal_try_except_ex_test1
  ${COMMON_TEST_LIBRARIES}
)
