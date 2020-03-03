set(SOURCES
  MapViewOfFile.cpp
)

add_executable(paltest_mapviewoffile_test3
  ${SOURCES}
)

add_dependencies(paltest_mapviewoffile_test3 coreclrpal)

target_link_libraries(paltest_mapviewoffile_test3
  ${COMMON_TEST_LIBRARIES}
)
