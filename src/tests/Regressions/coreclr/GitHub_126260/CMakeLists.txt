# The test is valid only on Unix, where the runtime uses a signal to suspend threads.
# Mobile and wasm targets are excluded to match the test project.
if (CLR_CMAKE_TARGET_UNIX AND NOT (CLR_CMAKE_TARGET_ANDROID OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_ARCH_WASM))
  include_directories(${INC_PLATFORM_DIR})

  add_library(nativetest126260 ${TEST_LIB_TYPE} nativetest126260.cpp)

  install(TARGETS nativetest126260 DESTINATION bin)
endif()
