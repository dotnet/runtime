if(CLR_CMAKE_HOST_WIN32)
  #use static crt
  add_definitions(-MT)
endif(CLR_CMAKE_HOST_WIN32)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(${CLR_DIR}/src/inc/llvm)

add_definitions(-DPAL_STDCPP_COMPAT)

if(CLR_CMAKE_TARGET_ALPINE_LINUX)
    add_definitions(-DTARGET_ALPINE_LINUX)
endif(CLR_CMAKE_TARGET_ALPINE_LINUX)

set(DBGUTIL_SOURCES
    dbgutil.cpp
)

if(CLR_CMAKE_TARGET_LINUX)
    list(APPEND DBGUTIL_SOURCES
        elfreader.cpp
    )
endif(CLR_CMAKE_TARGET_LINUX)

add_library_clr(dbgutil STATIC ${DBGUTIL_SOURCES})
