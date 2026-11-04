set(CMAKE_INCLUDE_CURRENT_DIR ON)

if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_WIN32 OR CLR_CMAKE_HOST_APPLE)
  include_directories(${CLR_DIR}/inc/llvm)
endif()

set(DBGUTIL_SOURCES
    dbgutil.cpp
    elfreader.cpp
    machoreader.cpp
    symbol.cpp
)

add_library_clr(dbgutil STATIC ${DBGUTIL_SOURCES})
