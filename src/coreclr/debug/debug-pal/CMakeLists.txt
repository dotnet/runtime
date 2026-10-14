include_directories(../inc)
include_directories(../../pal/inc)
include_directories(${CLR_SRC_NATIVE_DIR})

add_definitions(-DFEATURE_CORECLR)
add_definitions(-DFEATURE_PERFTRACING)
add_definitions(-DFEATURE_PERFTRACING_STANDALONE_PAL)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DWIN32_LEAN_AND_MEAN)
    include_directories(../../inc) #needed for warning control

    if(CLR_CMAKE_TARGET_WIN32)
        set(DEBUG_PAL_REFEREENCE_DIAGNOSTICSERVER ON)
        set(DEBUG_PAL_SOURCES
            ${EVENTPIPE_PAL_SOURCES}
            win/twowaypipe.cpp
            win/processdescriptor.cpp
        )
    else(CLR_CMAKE_TARGET_WIN32)
        set(DEBUG_PAL_SOURCES
            dummy/twowaypipe.cpp
        )
    endif(CLR_CMAKE_TARGET_WIN32)

    list(APPEND DEBUG_PAL_SOURCES win/wait.cpp)
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_UNIX)
    if (NOT CLR_CMAKE_TARGET_WASI)
        # ds-ipc-pal-socket.c needs UNIX/TCP sockets; re-enable once
        # WASI PERFTRACING is wired. Tracked by https://github.com/dotnet/runtime/issues/130383

        set(DEBUG_PAL_REFEREENCE_DIAGNOSTICSERVER ON)
    endif()

    add_definitions(-DPAL_IMPLEMENTATION)
    add_definitions(-D_POSIX_C_SOURCE=200809L)

    include_directories(../../inc) # for volatile.h

    set(DEBUG_PAL_SOURCES
      ${EVENTPIPE_PAL_SOURCES}
      unix/twowaypipe.cpp
      unix/processdescriptor.cpp
    )

    if (NOT CLR_CMAKE_TARGET_ARCH_WASM)
        list(APPEND DEBUG_PAL_SOURCES unix/wait.cpp)
        # Strict POSIX mode hides pipe2 on BSD even when the platform supports it.
        set_property(SOURCE unix/wait.cpp APPEND PROPERTY COMPILE_OPTIONS -U_POSIX_C_SOURCE)
    endif()

endif(CLR_CMAKE_HOST_UNIX)

add_library_clr(debug-pal OBJECT ${DEBUG_PAL_SOURCES})
target_link_libraries(debug-pal PRIVATE minipal)

if(CLR_CMAKE_HOST_UNIX)
    if(HAVE_PIPE2)
        target_compile_definitions(debug-pal PRIVATE HAVE_PIPE2=1)
    else()
        target_compile_definitions(debug-pal PRIVATE HAVE_PIPE2=0)
    endif()
endif(CLR_CMAKE_HOST_UNIX)

if (DEBUG_PAL_REFEREENCE_DIAGNOSTICSERVER)
    target_link_libraries(debug-pal PRIVATE dn-diagnosticserver-pal)

    # The DiagnosticServer Runtime implementation in CoreCLR is C++,
    # but the Diagnostic Server PAL is written in C.
    # Override those files to build as C++.
    get_property(ds_pal_sources TARGET dn-diagnosticserver-pal PROPERTY INTERFACE_SOURCES)
    set_source_files_properties(${ds_pal_sources} PROPERTIES LANGUAGE CXX)
endif()
