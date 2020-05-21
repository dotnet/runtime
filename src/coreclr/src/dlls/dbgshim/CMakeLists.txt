add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

include_directories(${CLR_DIR}/src/debug/shim)

set(DBGSHIM_SOURCES
    dbgshim.cpp
)

if(CLR_CMAKE_HOST_WIN32)
    # Use static crt
    add_definitions(-MT)
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
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
    # This option is necessary to ensure that the overloaded delete operator defined inside
    # of the utilcode will be used instead of the standard library delete operator.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic")

    # Add linker exports file option
    if(CLR_CMAKE_HOST_SUNOS)
        set(EXPORTS_LINKER_OPTION -Wl,-M,${EXPORTS_FILE})
    else(CLR_CMAKE_HOST_SUNOS)
        set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_SUNOS)
endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)

if(CLR_CMAKE_HOST_OSX)
    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_OSX)

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
        coreclrpal
        mscorrc
        palrt
    )
endif(CLR_CMAKE_HOST_WIN32)

target_link_libraries(dbgshim ${DBGSHIM_LIBRARIES})

# add the install targets
install_clr(TARGETS dbgshim ADDITIONAL_DESTINATION sharedFramework)
