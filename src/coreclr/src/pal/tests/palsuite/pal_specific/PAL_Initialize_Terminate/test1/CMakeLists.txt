set(SOURCES
  PAL_Initialize_Terminate.cpp
)

add_executable(paltest_pal_initialize_terminate_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_initialize_terminate_test1 coreclrpal)

target_link_libraries(paltest_pal_initialize_terminate_test1
  ${COMMON_TEST_LIBRARIES}
)
