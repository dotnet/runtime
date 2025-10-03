add_subdirectory(SOS)

if (NOT CLR_CMAKE_TARGET_BROWSER)
  add_subdirectory(superpmi)
endif()
