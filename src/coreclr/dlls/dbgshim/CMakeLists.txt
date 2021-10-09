add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

include_directories(${CLR_DIR}/debug/shim)

set(DBGSHIM_SOURCES
    dbgshim.cpp
)

if(CLR_CMAKE_HOST_WIN32)
    # Use static crt
    set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded)
    add_definitions(-DFX_VER_INTERNALNAME_STR=dbgshim.dll)
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND DBGSHIM_SOURCES
        dbgshim.rc
    )

    preprocess_file(${CMAKE_CURRENT_SOURCE_DIR}/dbgshim.ntdef ${CMAKE_CURRENT_BINARY_DIR}/dbgshim.def)
    list(APPEND DBGSHIM_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/dbgshim.def)
else(CLR_CMAKE_TARGET_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/dbgshim_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/dbgshim.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
        # This option is necessary to ensure that the overloaded delete operator defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic")

        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)
    endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)

    set_exports_linker_option(${EXPORTS_FILE})

endif(CLR_CMAKE_TARGET_WIN32)

add_library_clr(dbgshim SHARED ${DBGSHIM_SOURCES})

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(dbgshim_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(dbgshim dbgshim_exports)

    set_property(TARGET dbgshim APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET dbgshim APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

set(DBGSHIM_LIBRARIES
    debug-pal
    debugshim
    corguids
    dbgutil
    utilcodestaticnohost
)

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND DBGSHIM_LIBRARIES
        kernel32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        uuid.lib
        user32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        WtsApi32.lib
        version.lib
        psapi.lib
    )
else()
    list(APPEND DBGSHIM_LIBRARIES
        mscorrc
        ${START_WHOLE_ARCHIVE}
        coreclrpal
        palrt
        ${END_WHOLE_ARCHIVE}
    )
endif(CLR_CMAKE_HOST_WIN32)

target_link_libraries(dbgshim ${DBGSHIM_LIBRARIES})

# add the install targets
install_clr(TARGETS dbgshim DESTINATIONS . sharedFramework COMPONENT runtime)
