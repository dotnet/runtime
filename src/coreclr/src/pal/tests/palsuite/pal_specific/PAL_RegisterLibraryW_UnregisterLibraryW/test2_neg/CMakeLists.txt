set(SOURCES
  reg_unreg_libraryw_neg.cpp
)

add_executable(paltest_reg_unreg_libraryw_neg
  ${SOURCES}
)

add_dependencies(paltest_reg_unreg_libraryw_neg coreclrpal)

target_link_libraries(paltest_reg_unreg_libraryw_neg
  ${COMMON_TEST_LIBRARIES}
)
