project(profiler)

include_directories(
  ${PROJECT_BINARY_DIR}/
  ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_BINARY_DIR}/../../mono/eglib
  ${CMAKE_CURRENT_SOURCE_DIR}/../..
  ${PROJECT_SOURCE_DIR}/../
  ${PROJECT_SOURCE_DIR}/../eglib
  ${PROJECT_SOURCE_DIR}/../sgen)

if(NOT DISABLE_LIBS)
  add_library(mono-profiler-aot-static STATIC aot.c helper.c)
  set_target_properties(mono-profiler-aot-static PROPERTIES OUTPUT_NAME mono-profiler-aot)
  install(TARGETS mono-profiler-aot-static LIBRARY)
endif()
