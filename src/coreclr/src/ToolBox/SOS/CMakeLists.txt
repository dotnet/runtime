if(CLR_CMAKE_TARGET_WIN32)
  if (CMAKE_GENERATOR MATCHES "Visual Studio .*")
    add_subdirectory(DacTableGen)
  endif()
endif(CLR_CMAKE_TARGET_WIN32)

_install(FILES SOS_README.md DESTINATION .)
