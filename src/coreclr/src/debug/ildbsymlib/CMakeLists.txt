if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  add_definitions(-MT)
endif(CLR_CMAKE_HOST_WIN32)

set( ILDBSYMLIB_SOURCES
  symread.cpp
  symbinder.cpp
  ildbsymbols.cpp
  symwrite.cpp
)

add_library_clr(ildbsymlib ${ILDBSYMLIB_SOURCES})

