# The test is valid only on macOS.
if (CLR_CMAKE_TARGET_OSX)
  include_directories(${INC_PLATFORM_DIR})

  add_library(nativetest132581 ${TEST_LIB_TYPE} nativetest132581.cpp)

  install(TARGETS nativetest132581 DESTINATION bin)
endif()
