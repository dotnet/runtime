set(MSCORDBI_SOURCES
  mscordbi.cpp
)  

if(WIN32)
    add_precompiled_header(stdafx.h stdafx.cpp MSCORDBI_SOURCES)
    
    add_definitions(-DFX_VER_INTERNALNAME_STR=mscordbi.dll)

    list(APPEND MSCORDBI_SOURCES
        Native.rc
    )

    set(DEF_SOURCES
      mscordbi.src
    )
    convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})

    preprocess_def_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)

    list(APPEND MSCORDBI_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)
endif(WIN32)

add_library(mscordbi SHARED
  ${MSCORDBI_SOURCES}
)

set(COREDBI_LIBRARIES
  debug-pal
  cordbdi
  utilcodestaticnohost
  ildbsymlib
  mdcompiler-dbi
  mdruntime-dbi
  mdruntimerw-dbi
  mddatasource_dbi
  corguids
) 

if(WIN32)
    list(APPEND COREDBI_LIBRARIES
        ipcmanager-staticcrt
        mdhotdata-staticcrt
        mdwinmd_dbi
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        ${STATIC_MT_CRT_LIB}
    )

    target_link_libraries(mscordbi ${COREDBI_LIBRARIES})

elseif(CLR_CMAKE_PLATFORM_UNIX)
    list(APPEND COREDBI_LIBRARIES
        mdhotdata_full
        palrt
        CoreClrPal
    )

    # COREDBI_LIBRARIES is mentioned twice because ld is one pass linker and will not find symbols
    # if they are defined after they are used. Having all libs twice makes sure that ld will actually
    # find all symbols.
    target_link_libraries(mscordbi ${COREDBI_LIBRARIES} ${COREDBI_LIBRARIES})
    
endif(WIN32)



# add the install targets
install (TARGETS mscordbi DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordbi.pdb DESTINATION PDB)
endif(WIN32)
