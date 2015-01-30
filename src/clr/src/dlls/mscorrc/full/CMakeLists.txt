add_library(mscorrc.debug SHARED
  ../include.rc
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.debug.dll)

# add the install targets
install (TARGETS mscorrc.debug DESTINATION .)
