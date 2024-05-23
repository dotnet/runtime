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
  if(HOST_ANDROID OR HOST_IOS OR HOST_TVOS OR HOST_S390X)
    # Build the logging profiler only for certain platforms
    add_library(mono-profiler-log SHARED helper.c log.c log-args.c)
    target_compile_definitions(mono-profiler-log PRIVATE -DMONO_DLL_EXPORT)
    target_link_libraries(mono-profiler-log PRIVATE monosgen-shared monoapi eglib_objects zlib)
    if(HOST_ANDROID)
      target_link_libraries(mono-profiler-log PRIVATE log)
    endif()
    install(TARGETS mono-profiler-log LIBRARY)

    add_library(mono-profiler-log-static STATIC helper.c log.c log-args.c)
    target_link_libraries(mono-profiler-log-static PRIVATE monoapi)
    set_target_properties(mono-profiler-log-static PROPERTIES OUTPUT_NAME mono-profiler-log)
    install(TARGETS mono-profiler-log-static LIBRARY)

    if(NOT DISABLE_LOG_PROFILER_GZ)
      target_link_libraries(mono-profiler-log PRIVATE zlib)
    endif()
  endif()

  if(NOT HOST_WASI)
    add_library(mono-profiler-aot-static STATIC aot.c helper.c)
    target_link_libraries(mono-profiler-aot-static PRIVATE monoapi)
    set_target_properties(mono-profiler-aot-static PROPERTIES OUTPUT_NAME mono-profiler-aot)
    install(TARGETS mono-profiler-aot-static LIBRARY)
  endif()

  if(HOST_BROWSER)
    add_library(mono-profiler-browser-static STATIC browser.c)
    target_link_libraries(mono-profiler-browser-static PRIVATE monoapi)
    set_target_properties(mono-profiler-browser-static PROPERTIES OUTPUT_NAME mono-profiler-browser)
    install(TARGETS mono-profiler-browser-static LIBRARY)
  endif()
endif()
