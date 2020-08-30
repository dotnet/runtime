
include_directories(../inc)
include_directories(../../pal/inc)

add_definitions(-DPAL_STDCPP_COMPAT)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    include_directories(../../inc) #needed for warning control

    if(CLR_CMAKE_TARGET_WIN32)
        set(TWO_WAY_PIPE_SOURCES
            win/diagnosticsipc.cpp
            win/twowaypipe.cpp
            win/processdescriptor.cpp
        )
    else(CLR_CMAKE_TARGET_WIN32)
        set(TWO_WAY_PIPE_SOURCES
            dummy/twowaypipe.cpp
        )
    endif(CLR_CMAKE_TARGET_WIN32)
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_UNIX)

    add_definitions(-DPAL_IMPLEMENTATION)
    add_definitions(-D_POSIX_C_SOURCE=200809L)

    set(TWO_WAY_PIPE_SOURCES
      unix/diagnosticsipc.cpp
      unix/twowaypipe.cpp
      unix/processdescriptor.cpp
    )

endif(CLR_CMAKE_HOST_UNIX)

_add_library(debug-pal_obj OBJECT ${TWO_WAY_PIPE_SOURCES})
add_library(debug-pal INTERFACE)
target_sources(debug-pal INTERFACE $<TARGET_OBJECTS:debug-pal_obj>)
