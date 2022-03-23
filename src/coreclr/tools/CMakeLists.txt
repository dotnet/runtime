add_subdirectory(SOS)
add_subdirectory(superpmi)

if (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64)
  add_subdirectory(StressLogAnalyzer)
endif()
