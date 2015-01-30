add_library(mscorrc SHARED
  ../mscorrc.small.rc
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

# add the install targets
install (TARGETS mscorrc DESTINATION .)