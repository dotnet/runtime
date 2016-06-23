include(${CLR_DIR}/dac.cmake)

add_definitions(-DFEATURE_NO_HOST)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

set(CLR_DAC_SOURCES
    mscordac.cpp
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscordaccore.dll)

if(WIN32)
    list(APPEND CLR_DAC_SOURCES
        Native.rc
    )
    set(DEF_SOURCES
        mscordac.src
    )

    #Preprocess exports definition file
    preprocess_def_file(${CMAKE_CURRENT_SOURCE_DIR}/${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordac.def)

    #create target to add file dependency on mscordac.def
    add_custom_target(mscordaccore_def DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/mscordac.def)

    # No library groups for Win32
    set(START_LIBRARY_GROUP)
    set(END_LIBRARY_GROUP)
else(WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/mscordac_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/mscordac.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
endif(WIN32)

if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)
    # This option is necessary to ensure that the overloaded delete operator defined inside
    # of the utilcode will be used instead of the standard library delete operator.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Xlinker -Bsymbolic-functions")    

    # The following linked options can be inserted into the linker libraries list to 
    # ensure proper resolving of circular references between a subset of the libraries.
    set(START_LIBRARY_GROUP -Wl,--start-group)
    set(END_LIBRARY_GROUP -Wl,--end-group)

    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)

if(CLR_CMAKE_PLATFORM_DARWIN)
    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_DARWIN)

add_library_clr(mscordaccore SHARED ${CLR_DAC_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_custom_target(mscordaccore_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(mscordaccore mscordaccore_exports)

    set_property(TARGET mscordaccore APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET mscordaccore APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_UNIX)

# IMPORTANT! Please do not rearrange the order of the libraries. The linker on Linux is
# order dependent and changing the order can result in undefined symbols in the shared 
# library.
set(COREDAC_LIBRARIES
    ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
    cee_dac
    cordbee_dac
    corguids
    daccess
    dbgutil
    mdcompiler_dac
    mdhotdata_dac
    mdruntime_dac
    mdruntimerw_dac
    strongname_dac
    utilcode_dac
    unwinder_dac
    gc_dac
    ${END_LIBRARY_GROUP} # End group of libraries that have circular references
)

if(WIN32)
    # mscordac.def should be generated before mscordaccore.dll is built
    add_dependencies(mscordaccore mscordaccore_def)

    # Generate export file
    add_custom_command(TARGET mscordaccore
        PRE_LINK
        COMMAND lib.exe /OUT:"${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordaccore.lib" /DEF:"${CMAKE_CURRENT_BINARY_DIR}/mscordac.def" "$<TARGET_FILE:daccess>" $<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/LTCG> ${STATIC_LIBRARY_FLAGS} "${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.dir/$<CONFIG>/mscordac.obj"
        COMMENT "Generating mscordaccore.exp export file"
    )

    set(COREDAC_LIBRARIES
        ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordaccore.exp # export file
        ${COREDAC_LIBRARIES}
        mdwinmd_dac
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
else(WIN32)
    list(APPEND COREDAC_LIBRARIES
        mscorrc_debug
        coreclrpal
        palrt
    )
endif(WIN32)

target_link_libraries(mscordaccore PRIVATE ${COREDAC_LIBRARIES})

# add the install targets
install_clr(mscordaccore)