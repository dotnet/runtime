set(SOURCES
  mapviewoffile.cpp
)

add_executable(paltest_mapviewoffile_test5
  ${SOURCES}
)

add_dependencies(paltest_mapviewoffile_test5 coreclrpal)

target_link_libraries(paltest_mapviewoffile_test5
  ${COMMON_TEST_LIBRARIES}
)
