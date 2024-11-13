# The test is valid only on Apple
if (CLR_CMAKE_TARGET_APPLE)
  include_directories(${INC_PLATFORM_DIR})

  add_library(nativetest102887 SHARED nativetest102887.cpp)

  # add the install targets
  install (TARGETS nativetest102887 DESTINATION bin)
endif ()