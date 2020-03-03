set(SOURCES
  LoadLibraryW.cpp
)

add_executable(paltest_loadlibraryw_test1
  ${SOURCES}
)

add_dependencies(paltest_loadlibraryw_test1 coreclrpal)

target_link_libraries(paltest_loadlibraryw_test1
  ${COMMON_TEST_LIBRARIES}
)
