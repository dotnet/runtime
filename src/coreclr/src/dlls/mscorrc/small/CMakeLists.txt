add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

add_library_clr(mscorrc SHARED
  ../mscorrc.small.rc
)

install_clr(TARGETS mscorrc)
install_clr(TARGETS mscorrc DESTINATION sharedFramework)
