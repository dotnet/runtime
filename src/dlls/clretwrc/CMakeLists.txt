include_directories(${CMAKE_CURRENT_BINARY_DIR})

if(WIN32)
    # remove /ltcg from resource-only libraries
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELEASE ${CMAKE_SHARED_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELEASE ${CMAKE_STATIC_LINKER_FLAGS_RELEASE})
    string(REPLACE "/LTCG" "" CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO})
    string(REPLACE "/LTCG" "" CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO ${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO})
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NOENTRY")
endif(WIN32)

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
