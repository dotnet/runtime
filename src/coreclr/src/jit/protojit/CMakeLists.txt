project(protojit)

remove_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)
add_definitions(-DALT_JIT)

add_library_clr(protojit
   SHARED
   ${SHARED_LIB_SOURCES}
)

set(RYUJIT_LINK_LIBRARIES
   utilcodestaticnohost
   gcinfo
)

if(CLR_CMAKE_PLATFORM_UNIX)
    list(APPEND RYUJIT_LINK_LIBRARIES
       mscorrc_debug
       coreclrpal
       palrt
    )
else()
    list(APPEND RYUJIT_LINK_LIBRARIES
       msvcrt.lib
       kernel32.lib
       advapi32.lib
       ole32.lib
       oleaut32.lib
       uuid.lib
       user32.lib
       version.lib
       shlwapi.lib
       bcrypt.lib
       crypt32.lib
       RuntimeObject.lib
    )
endif(CLR_CMAKE_PLATFORM_UNIX)

target_link_libraries(protojit
   ${RYUJIT_LINK_LIBRARIES}
)

# add the install targets
install (TARGETS protojit DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/protojit.pdb DESTINATION PDB)
endif(WIN32)
