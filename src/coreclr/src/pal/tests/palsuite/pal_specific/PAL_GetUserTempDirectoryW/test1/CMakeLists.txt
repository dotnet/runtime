set(SOURCES
  PAL_GetUserTempDirectoryW.cpp
)

add_executable(paltest_pal_getusertempdirectoryw_test1
  ${SOURCES}
)

add_dependencies(paltest_pal_getusertempdirectoryw_test1 coreclrpal)

target_link_libraries(paltest_pal_getusertempdirectoryw_test1
  ${COMMON_TEST_LIBRARIES}
)
