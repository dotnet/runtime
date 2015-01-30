set(MSCORDBI_SOURCES
  mscordbi.cpp
  Native.rc
)  

set(DEF_SOURCES
  mscordbi.src
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscordbi.dll)

convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})

preprocess_def_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)

set(MSCORDBI_SOURCES ${MSCORDBI_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)

add_library(mscordbi SHARED
  ${MSCORDBI_SOURCES}
)

set(COREDBI_LIBRARIES
  cordbdi
  utilcodestaticnohost
  ipcmanager-staticcrt
  ildbsymlib
  mdcompiler-dbi
  mdwinmd_dbi
  mdruntime-dbi
  mdruntimerw-dbi
  mdhotdata-staticcrt
  mddatasource_dbi
  corguids
) 

if(WIN32)
    set(COREDBI_LIBRARIES
        ${COREDBI_LIBRARIES}
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        ${STATIC_MT_CRT_LIB}
    )
endif(WIN32)

target_link_libraries(mscordbi ${COREDBI_LIBRARIES})

# add the install targets
install (TARGETS mscordbi DESTINATION .)
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordbi.pdb DESTINATION PDB)
