cmake_minimum_required(VERSION 2.8.12.2)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

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
