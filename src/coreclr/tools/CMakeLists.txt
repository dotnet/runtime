if (CLR_CMAKE_TARGET_WIN32 AND NOT CLR_CMAKE_CROSS_ARCH)
  add_subdirectory(GenClrDebugResource)
  add_subdirectory(InjectResource)

  install(EXPORT dactabletools DESTINATION dactabletools COMPONENT crosscomponents)
endif()
if ((NOT CLR_CMAKE_TARGET_ALPINE_LINUX) AND (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64))
  add_subdirectory(StressLogAnalyzer)
endif()