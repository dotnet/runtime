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
  if(HOST_ANDROID OR HOST_IOS OR HOST_TVOS)
    # Build the logging profiler only for mobile platforms
    add_library(mono-profiler-log SHARED helper.c log.c log-args.c)
    target_link_libraries(mono-profiler-log monosgen-shared eglib_objects)
    if(HOST_ANDROID)
      target_link_libraries(mono-profiler-log log)
    endif()
    install(TARGETS mono-profiler-log LIBRARY)

    add_library(mono-profiler-log-static STATIC helper.c log.c log-args.c)
    set_target_properties(mono-profiler-log-static PROPERTIES OUTPUT_NAME mono-profiler-log)
    install(TARGETS mono-profiler-log-static LIBRARY)
  endif()

  add_library(mono-profiler-aot-static STATIC aot.c helper.c)
  set_target_properties(mono-profiler-aot-static PROPERTIES OUTPUT_NAME mono-profiler-aot)
  install(TARGETS mono-profiler-aot-static LIBRARY)
endif()
