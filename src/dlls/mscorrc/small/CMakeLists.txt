add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

if(WIN32)

  add_library(mscorrc SHARED
    ../mscorrc.small.rc
  )

  # add the install targets
  install (TARGETS mscorrc DESTINATION .)
  install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscorrc.pdb DESTINATION PDB)

else()

  build_resources(${CMAKE_CURRENT_SOURCE_DIR}/../mscorrc.small.rc mscorrc TARGET_CPP_FILE)

  add_library(mscorrc STATIC
    ${TARGET_CPP_FILE}
  )

endif(WIN32)

