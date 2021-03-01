
include_directories(../inc)
include_directories(../../pal/inc)

add_definitions(-DPAL_STDCPP_COMPAT)


set(SHARED_EVENTPIPE_DIR ${CLR_SRC_NATIVE_DIR}/eventpipe)
include_directories(${SHARED_EVENTPIPE_DIR})
add_definitions(-DFEATURE_CORECLR)
add_definitions(-DFEATURE_PERFTRACING)
add_definitions(-DFEATURE_PERFTRACING_STANDALONE_PAL)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    include_directories(../../inc) #needed for warning control

    if(CLR_CMAKE_TARGET_WIN32)
            set (EVENTPIPE_PAL_SOURCES "${SHARED_EVENTPIPE_DIR}/ds-ipc-win32.c")
            set_source_files_properties(${EVENTPIPE_PAL_SOURCES} PROPERTIES LANGUAGE CXX)

        set(TWO_WAY_PIPE_SOURCES
            ${EVENTPIPE_PAL_SOURCES}
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

    set (EVENTPIPE_PAL_SOURCES "${SHARED_EVENTPIPE_DIR}/ds-ipc-posix.c")
    set_source_files_properties(${EVENTPIPE_PAL_SOURCES} PROPERTIES LANGUAGE CXX)
    if (CMAKE_VERSION VERSION_GREATER 3.11 OR CMAKE_VERSION VERSION_EQUAL 3.11)
        set_source_files_properties(${EVENTPIPE_PAL_SOURCES} PROPERTIES COMPILE_OPTIONS -xc++)
    else(CMAKE_VERSION VERSION_GREATER 3.11 OR CMAKE_VERSION VERSION_EQUAL 3.11)
        add_compile_options(-xc++)
    endif()

    set(TWO_WAY_PIPE_SOURCES
      ${EVENTPIPE_PAL_SOURCES}
      unix/twowaypipe.cpp
      unix/processdescriptor.cpp
    )

endif(CLR_CMAKE_HOST_UNIX)

_add_library(debug-pal_obj OBJECT ${TWO_WAY_PIPE_SOURCES})
add_library(debug-pal INTERFACE)
target_sources(debug-pal INTERFACE $<TARGET_OBJECTS:debug-pal_obj>)
