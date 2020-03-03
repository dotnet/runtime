set(SOURCES
  MapViewOfFile.cpp
)

add_executable(paltest_mapviewoffile_test1
  ${SOURCES}
)

add_dependencies(paltest_mapviewoffile_test1 coreclrpal)

target_link_libraries(paltest_mapviewoffile_test1
  ${COMMON_TEST_LIBRARIES}
)
