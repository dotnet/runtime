project(CoreConsole)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
 
set(CoreConsole_SOURCES coreconsole.cpp logger.cpp)
set(CoreConsole_RESOURCES native.rc)

add_definitions(-DFX_VER_INTERNALNAME_STR=CoreConsole.exe)

if(CLR_CMAKE_PLATFORM_UNIX)
    # This does not compile on Linux yet
    if(CAN_BE_COMPILED_ON_LINUX)
        _add_executable(CoreConsole
          ${CoreConsole_SOURCES}
          ${CoreConsole_RESOURCES}
        )
    endif(CAN_BE_COMPILED_ON_LINUX)

else()
    _add_executable(CoreConsole
      ${CoreConsole_SOURCES}
      ${CoreConsole_RESOURCES}
    )

    target_link_libraries(CoreConsole
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )

    # Can't compile on linux yet so only add for windows
    install_clr(CoreConsole)

endif(CLR_CMAKE_PLATFORM_UNIX)