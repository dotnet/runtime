set(SOURCES
  mapviewoffile.cpp
)

add_executable(paltest_mapviewoffile_test4
  ${SOURCES}
)

add_dependencies(paltest_mapviewoffile_test4 coreclrpal)

target_link_libraries(paltest_mapviewoffile_test4
  ${COMMON_TEST_LIBRARIES}
)
