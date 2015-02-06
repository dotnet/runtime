include_directories(${CMAKE_CURRENT_BINARY_DIR})
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")

add_definitions(-DFX_VER_INTERNALNAME_STR=clretwrc.dll)

add_library(clretwrc SHARED
  clretwrc.rc
)

#Compile clretwall.man file to create clretwall.rc resource file
add_custom_command(
  TARGET clretwrc 
  PRE_BUILD
  COMMAND mc -h ${CMAKE_CURRENT_BINARY_DIR} -r ${CMAKE_CURRENT_BINARY_DIR} -b -co -um -p FireEtw ${VM_DIR}/ClrEtwAll.man
  DEPENDS ${VM_DIR}/ClrEtwAll.man
  COMMENT "MessageCompile ${VM_DIR}/ClrEtwAll.man"
)

# add the install targets
install (TARGETS clretwrc DESTINATION .)

if(WIN32)
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/clretwrc.pdb DESTINATION PDB)
endif(WIN32)
