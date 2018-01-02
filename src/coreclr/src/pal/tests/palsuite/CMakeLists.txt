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

add_compile_options(-Wno-incompatible-pointer-types-discards-qualifiers)
add_compile_options(-Wno-int-to-void-pointer-cast)

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
  add_subdirectory(${GENERATED_EVENTING_DIR}/eventprovider/tests ${CMAKE_CURRENT_BINARY_DIR}/eventprovider)
endif(FEATURE_EVENT_TRACE)
