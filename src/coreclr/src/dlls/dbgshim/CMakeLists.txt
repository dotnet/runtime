add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)

include_directories(${CLR_DIR}/src/debug/shim)

set(DBGSHIM_SOURCES
    dbgshim.cpp
)

if(WIN32)
    # Use static crt
    add_definitions(-MT) 
    add_definitions(-DFX_VER_INTERNALNAME_STR=dbgshim.dll)

    list(APPEND DBGSHIM_SOURCES
        dbgshim.rc
    )

    preprocess_def_file(${CMAKE_CURRENT_SOURCE_DIR}/dbgshim.ntdef ${CMAKE_CURRENT_BINARY_DIR}/dbgshim.def)
    list(APPEND DBGSHIM_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/dbgshim.def)
endif(WIN32)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)
    # This option is necessary to ensure that the overloaded delete operator defined inside
    # of the utilcode will be used instead of the standard library delete operator.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")
endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)

add_library_clr(dbgshim SHARED ${DBGSHIM_SOURCES})

set(DBGSHIM_LIBRARIES
    debug-pal
    debugshim
    corguids
    dbgutil
    utilcodestaticnohost
)

if(WIN32)
    list(APPEND DBGSHIM_LIBRARIES
        kernel32.lib
        ${STATIC_MT_CRT_LIB}
        uuid.lib
        user32.lib
        advapi32.lib
        oleaut32.lib
        WtsApi32.lib
        version.lib
        psapi.lib
    )

    # ARM64_TODO: Enable this for Windows Arm64
    if (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
        list(APPEND DBGSHIM_LIBRARIES 
            ${STATIC_MT_VCRT_LIB}
        )
    endif()
  
else()
    list(APPEND DBGSHIM_LIBRARIES
        coreclrpal
        mscorrc_debug
        palrt
    )
endif(WIN32)

target_link_libraries(dbgshim ${DBGSHIM_LIBRARIES})

# add the install targets
install (TARGETS dbgshim DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/dbgshim.pdb DESTINATION PDB)
endif(WIN32)
