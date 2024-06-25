include_directories(../inc)
include_directories(../../pal/inc)

add_definitions(-DFEATURE_CORECLR)
add_definitions(-DFEATURE_PERFTRACING)
add_definitions(-DFEATURE_PERFTRACING_STANDALONE_PAL)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    include_directories(../../inc) #needed for warning control

    if(CLR_CMAKE_TARGET_WIN32)
        set(DEBUG_PAL_REFEREENCE_DIAGNOSTICSERVER ON)
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
    set(DEBUG_PAL_REFEREENCE_DIAGNOSTICSERVER ON)

    add_definitions(-DPAL_IMPLEMENTATION)
    add_definitions(-D_POSIX_C_SOURCE=200809L)

    set(TWO_WAY_PIPE_SOURCES
      ${EVENTPIPE_PAL_SOURCES}
      unix/twowaypipe.cpp
      unix/processdescriptor.cpp
    )

endif(CLR_CMAKE_HOST_UNIX)

add_library_clr(debug-pal OBJECT ${TWO_WAY_PIPE_SOURCES})

if (DEBUG_PAL_REFEREENCE_DIAGNOSTICSERVER)
    target_link_libraries(debug-pal PRIVATE dn-diagnosticserver-pal)

    # The DiagnosticServer Runtime implementation in CoreCLR is C++,
    # but the Diagnostic Server PAL is written in C.
    # Override those files to build as C++.
    get_property(ds_pal_sources TARGET dn-diagnosticserver-pal PROPERTY INTERFACE_SOURCES)
    set_source_files_properties(${ds_pal_sources} PROPERTIES LANGUAGE CXX)
endif()
