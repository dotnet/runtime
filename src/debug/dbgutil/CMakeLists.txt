if(WIN32)
  #use static crt
  add_definitions(-MT)
endif(WIN32)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

set(DBGUTIL_SOURCES
    dbgutil.cpp
)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_library_clr(dbgutil STATIC ${DBGUTIL_SOURCES})
