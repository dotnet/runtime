add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

add_library_clr(mscorrc SHARED
  ../mscorrc.small.rc
)

# add the install targets
install (TARGETS mscorrc DESTINATION .)
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscorrc.pdb DESTINATION PDB)


