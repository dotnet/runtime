add_subdirectory(crossgen)

if(WIN32)
  add_subdirectory(GenClrDebugResource)
  add_subdirectory(InjectResource)
endif(WIN32)
