set(SOURCES
  setcurrentdirectorya.cpp
)

add_executable(paltest_setcurrentdirectorya_test3
  ${SOURCES}
)

add_dependencies(paltest_setcurrentdirectorya_test3 coreclrpal)

target_link_libraries(paltest_setcurrentdirectorya_test3
  ${COMMON_TEST_LIBRARIES}
)
