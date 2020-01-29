set(SOURCES
  CreateFileMapping_neg.cpp
)

add_executable(paltest_createfilemappingw_createfilemapping_neg1
  ${SOURCES}
)

add_dependencies(paltest_createfilemappingw_createfilemapping_neg1 coreclrpal)

target_link_libraries(paltest_createfilemappingw_createfilemapping_neg1
  ${COMMON_TEST_LIBRARIES}
)
