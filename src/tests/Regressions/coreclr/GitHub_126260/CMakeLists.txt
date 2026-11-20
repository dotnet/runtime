# The test is valid only on Unix, where the runtime uses a signal to suspend threads.
if (CLR_CMAKE_TARGET_UNIX)
  include_directories(${INC_PLATFORM_DIR})

  add_library(nativetest126260 ${TEST_LIB_TYPE} nativetest126260.cpp)

  install(TARGETS nativetest126260 DESTINATION bin)
endif()
