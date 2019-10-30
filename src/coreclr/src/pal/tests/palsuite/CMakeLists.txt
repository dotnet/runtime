cmake_minimum_required(VERSION 2.8.12.2)

project(PALTESTSUITE)

include_directories(${PALTESTSUITE_SOURCE_DIR}/common)

# All test will link against these libraries:
# pthread and m are part of the Android C library (bionic),
# so we don't need to link them seperately
if(NOT CLR_CMAKE_PLATFORM_ANDROID)
  list(APPEND COMMON_TEST_LIBRARIES pthread)
  list(APPEND COMMON_TEST_LIBRARIES m)
endif()

list(APPEND COMMON_TEST_LIBRARIES coreclrpal)

if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
  add_compile_options(-Wno-incompatible-pointer-types-discards-qualifiers)
  add_compile_options(-Wno-int-to-void-pointer-cast)
elseif (CMAKE_CXX_COMPILER_ID STREQUAL "GNU")
  add_compile_options(-Wno-sign-compare)
  add_compile_options(-Wno-narrowing)
  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-fpermissive>)
  add_compile_options(-Wno-int-to-pointer-cast)
endif()

# TODO: make these tests compile
# add_subdirectory(composite)
add_subdirectory(exception_handling)

add_subdirectory(c_runtime)
add_subdirectory(debug_api)
add_subdirectory(filemapping_memmgt)
add_subdirectory(file_io)
add_subdirectory(loader)
add_subdirectory(locale_info)
add_subdirectory(miscellaneous)
add_subdirectory(pal_specific)
add_subdirectory(samples)
add_subdirectory(threading)

if(FEATURE_EVENT_TRACE)
  add_subdirectory(eventprovider)
endif(FEATURE_EVENT_TRACE)
