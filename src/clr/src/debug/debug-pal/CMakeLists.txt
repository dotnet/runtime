
include_directories(../inc)
include_directories(../../pal/inc)

add_definitions(-DPAL_STDCPP_COMPAT=1)

if(WIN32)
    #use static crt
    add_definitions(-MT) 
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    include_directories(../../inc) #needed for warning control

    set(TWO_WAY_PIPE_SOURCES 
        win/twowaypipe.cpp
        win/processdescriptor.cpp
    )
endif(WIN32)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)

    add_definitions(-DFEATURE_PAL)
    add_definitions(-DPAL_IMPLEMENTATION)
    add_definitions(-D_POSIX_C_SOURCE=200809L)

    set(TWO_WAY_PIPE_SOURCES
      unix/twowaypipe.cpp
      unix/processdescriptor.cpp
    )

endif(CLR_CMAKE_PLATFORM_UNIX)

_add_library(debug-pal STATIC ${TWO_WAY_PIPE_SOURCES})
