add_subdirectory(SOS)

if (NOT CLR_CMAKE_TARGET_BROWSER)
  add_subdirectory(superpmi)
endif()

if (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64)
  add_subdirectory(StressLogAnalyzer)
endif()
