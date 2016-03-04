project(CoreRun)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
 
set(CoreRun_SOURCES corerun.cpp logger.cpp)
set(CoreRun_RESOURCES native.rc)

add_definitions(-DFX_VER_INTERNALNAME_STR=CoreRun.exe)

if(CLR_CMAKE_PLATFORM_UNIX)
    # This does not compile on Linux yet
    if(CAN_BE_COMPILED_ON_LINUX)
        add_executable(CoreRun
          ${CoreRun_SOURCES}
          ${CoreRun_RESOURCES}
        )
    endif(CAN_BE_COMPILED_ON_LINUX)

else()
    add_executable(CoreRun
      ${CoreRun_SOURCES}
      ${CoreRun_RESOURCES}
    )

    target_link_libraries(CoreRun
        utilcodestaticnohost
        advapi32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        msvcrt.lib
    )

    # Can't compile on linux yet so only add for windows
    # add the install targets
    install (TARGETS CoreRun DESTINATION .)
    
    # We will generate PDB only for the debug configuration
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/CoreRun.pdb DESTINATION PDB)

endif(CLR_CMAKE_PLATFORM_UNIX)