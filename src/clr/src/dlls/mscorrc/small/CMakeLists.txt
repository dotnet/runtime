add_library(mscorrc SHARED
  ../mscorrc.small.rc
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

# add the install targets
install (TARGETS mscorrc DESTINATION .)
if(WIN32)
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscorrc.pdb DESTINATION PDB)
endif(WIN32)