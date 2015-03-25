add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.dll)

if(WIN32)
add_library(mscorrc SHARED
  ../mscorrc.small.rc
)

# add the install targets
install (TARGETS mscorrc DESTINATION .)

install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscorrc.pdb DESTINATION PDB)

else()

build_resources(${CMAKE_CURRENT_SOURCE_DIR}/../mscorrc.small.rc mscorrc TARGET_RESOURCE_FILE)

add_custom_target(
    mscorrc ALL
    DEPENDS ${TARGET_RESOURCE_FILE}
)

install (FILES ${TARGET_RESOURCE_FILE} DESTINATION en_US/LC_MESSAGES/)

endif(WIN32)

