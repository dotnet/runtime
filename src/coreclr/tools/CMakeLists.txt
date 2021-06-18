if (CLR_CMAKE_TARGET_WIN32 AND NOT CLR_CMAKE_CROSS_ARCH)
  add_subdirectory(GenClrDebugResource)
  add_subdirectory(InjectResource)

  install(EXPORT dactabletools DESTINATION dactabletools COMPONENT crosscomponents)
endif()
