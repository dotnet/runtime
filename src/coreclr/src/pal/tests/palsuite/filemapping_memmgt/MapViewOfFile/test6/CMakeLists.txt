set(SOURCES
  mapviewoffile.cpp
)

add_executable(paltest_mapviewoffile_test6
  ${SOURCES}
)

add_dependencies(paltest_mapviewoffile_test6 coreclrpal)

target_link_libraries(paltest_mapviewoffile_test6
  ${COMMON_TEST_LIBRARIES}
)
