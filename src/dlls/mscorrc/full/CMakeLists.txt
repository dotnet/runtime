add_definitions(-DFX_VER_INTERNALNAME_STR=mscorrc.debug.dll)

if(WIN32)
add_library(mscorrc.debug SHARED
  ../include.rc
)

# add the install targets
install (TARGETS mscorrc.debug DESTINATION .)

install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscorrc.debug.pdb DESTINATION PDB)

else()

build_resources(${CMAKE_CURRENT_SOURCE_DIR}/../include.rc mscorrc.debug TARGET_RESOURCE_FILE)

add_custom_target(
    mscorrc.debug ALL
    DEPENDS ${TARGET_RESOURCE_FILE}
)

install (FILES ${TARGET_RESOURCE_FILE} DESTINATION en_US/LC_MESSAGES/)

endif(WIN32)

