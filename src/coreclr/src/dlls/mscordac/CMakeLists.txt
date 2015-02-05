include(${CLR_DIR}/dac.cmake)

add_definitions(-DFEATURE_NO_HOST)

set(CLR_DAC_SOURCES
    mscordac.cpp
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscordaccore.dll)

if(WIN32)
    set(CLR_DAC_SOURCES ${CLR_DAC_SOURCES}
        Native.rc
    )
    set(DEF_SOURCES
        mscordac.src
    )

    #Preprocess exports definition file
    preprocess_def_file(${CMAKE_CURRENT_SOURCE_DIR}/${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordac.def)

    #create target to add file dependency on mscordac.def
    add_custom_target(mscordaccore_def DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/mscordac.def)
endif(WIN32)

add_library(mscordaccore SHARED ${CLR_DAC_SOURCES})

set(COREDAC_LIBRARIES
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
)

if(WIN32)
    # mscordac.def should be generated before mscordaccore.dll is built
    add_dependencies(mscordaccore mscordaccore_def)

    # Generate export file
    add_custom_command(TARGET mscordaccore
        PRE_LINK
        COMMAND lib.exe /OUT:${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordaccore.lib /DEF:${CMAKE_CURRENT_BINARY_DIR}/mscordac.def $<TARGET_FILE:daccess> ${STATIC_LIBRARY_FLAGS} ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.dir/$<CONFIG>/mscordac.obj
        COMMENT "Generating mscordaccore.exp export file"
        VERBATIM
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
else(WIN32)
    set(COREDAC_LIBRARIES
        ${COREDAC_LIBRARIES}
        palrt
    )
endif(WIN32)

target_link_libraries(mscordaccore ${COREDAC_LIBRARIES})

# add the install targets
install (TARGETS mscordaccore DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordaccore.pdb DESTINATION PDB)
endif(WIN32)
