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
    if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)
        # This option is necessary to ensure that the overloaded delete operator defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")

        # The following linked options can be inserted into the linker libraries list to 
        # ensure proper resolving of circular references between a subset of the libraries.
        set(START_LIBRARY_GROUP -Wl,--start-group)
        set(END_LIBRARY_GROUP -Wl,--end-group)
    endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)
endif(WIN32)

add_library_clr(mscordaccore SHARED ${CLR_DAC_SOURCES})

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
    )

    # ARM64_TODO: Enable this for Windows Arm64
    if (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
        list(APPEND COREDAC_LIBRARIES 
          ${STATIC_MT_VCRT_LIB}
        )
    endif()

else(WIN32)
    list(APPEND COREDAC_LIBRARIES
        mscorrc_debug
        coreclrpal
        palrt
    )
endif(WIN32)

target_link_libraries(mscordaccore ${COREDAC_LIBRARIES})

# add the install targets
install (TARGETS mscordaccore DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordaccore.pdb DESTINATION PDB)
endif(WIN32)
