set(VM_SOURCES_MONO 
    mono_coreclr.cpp
	)
convert_to_absolute_path(VM_SOURCES_MONO ${VM_SOURCES_MONO})

add_definitions(-DENABLE_MONO=1)
add_definitions(-DPLATFORM_STANDALONE=1)
add_definitions(-DUNITY_STANDALONE=1)

if(WIN32)
  add_definitions(-DPLATFORM_WIN=1)
endif()

if(CLR_CMAKE_PLATFORM_DARWIN)
  add_definitions(-DPLATFORM_OSX=1)
endif()

add_library_clr(mono ${VM_SOURCES_MONO})

add_dependencies(mono eventing_headers)
