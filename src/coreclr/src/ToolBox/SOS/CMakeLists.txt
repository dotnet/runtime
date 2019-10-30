if(WIN32)
  if (CMAKE_GENERATOR MATCHES "Visual Studio .*")
    add_subdirectory(DacTableGen)
  endif()    
endif(WIN32)

_install(FILES SOS_README.md DESTINATION .)
