if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  add_definitions(-MT)
endif(CLR_CMAKE_HOST_WIN32)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(DBGUTIL_SOURCES
    dbgutil.cpp
)

add_library_clr(dbgutil STATIC ${DBGUTIL_SOURCES})
