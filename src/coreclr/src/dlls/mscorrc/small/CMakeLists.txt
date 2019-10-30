add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

add_library_clr(mscorrc SHARED
  ../mscorrc.small.rc
)

# add the install targets
install_clr (mscorrc)