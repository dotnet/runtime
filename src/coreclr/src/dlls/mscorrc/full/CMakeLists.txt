add_library(mscorrc.debug SHARED
  ../include.rc
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.debug.dll)

# add the install targets
install (TARGETS mscorrc.debug DESTINATION .)

if(WIN32)
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscorrc.debug.pdb DESTINATION PDB)
endif(WIN32)