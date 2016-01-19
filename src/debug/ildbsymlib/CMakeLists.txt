if(WIN32)
  #use static crt
  add_definitions(-MT) 
endif(WIN32)

set( ILDBSYMLIB_SOURCES
  symread.cpp
  symbinder.cpp
  ildbsymbols.cpp
  symwrite.cpp
)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_library_clr(ildbsymlib ${ILDBSYMLIB_SOURCES})

