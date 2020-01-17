set(SOURCES
  GetTempPathW.cpp
)

add_executable(paltest_gettemppathw_test1
  ${SOURCES}
)

add_dependencies(paltest_gettemppathw_test1 coreclrpal)

target_link_libraries(paltest_gettemppathw_test1
  ${COMMON_TEST_LIBRARIES}
)
