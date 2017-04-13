cmake_minimum_required(VERSION 2.8.12.2)

project(COREPAL)

if (WIN32)
  set(FEATURE_EVENT_TRACE 1)
endif()
if(CLR_CMAKE_PLATFORM_LINUX)
  if(CLR_CMAKE_TARGET_ARCH_AMD64)
    set(FEATURE_EVENT_TRACE 1)
  elseif(CLR_CMAKE_TARGET_ARCH_ARM)
    if(NOT(CLR_CMAKE_TARGET_TIZEN_LINUX))
      set(FEATURE_EVENT_TRACE 1)
    endif()
  endif()
endif()

include_directories(${COREPAL_SOURCE_DIR}/inc)
include_directories(${COREPAL_SOURCE_DIR}/src)
include_directories(${COREPAL_SOURCE_DIR}/../inc)

add_compile_options(-fexceptions)

add_subdirectory(src)

if(CLR_CMAKE_BUILD_TESTS)
  add_subdirectory(tests)
endif(CLR_CMAKE_BUILD_TESTS)

