project(ryujit)
add_definitions(-DFEATURE_NO_HOST)
add_definitions(-DSELF_NO_HOST)
add_definitions(-DFEATURE_READYTORUN_COMPILER)
remove_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)

if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)
    # This is required to force using our own PAL, not one that we are loaded with.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")
endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)

add_library_clr(ryujit
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

target_link_libraries(ryujit
   ${RYUJIT_LINK_LIBRARIES}
)

# add the install targets
install (TARGETS ryujit DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/ryujit.pdb DESTINATION PDB)
endif(WIN32)
