set(SOURCES
  MapViewOfFile.cpp
)

add_executable(paltest_mapviewoffile_test2
  ${SOURCES}
)

add_dependencies(paltest_mapviewoffile_test2 coreclrpal)

target_link_libraries(paltest_mapviewoffile_test2
  ${COMMON_TEST_LIBRARIES}
)
