project(crossgen)

include(${CLR_DIR}/crossgen.cmake)

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include_directories(../util)
include_directories(../../pal/prebuilt/corerror)

set(crossgen_SOURCES crossgen.cpp ../util/consoleargs.cpp)
set(crossgen_RESOURCES Native.rc)

add_definitions(-DFX_VER_INTERNALNAME_STR=crossgen.exe)
add_definitions(-DNO_NGENPDB)

if(CLR_CMAKE_PLATFORM_UNIX)
    # This does not compile on Linux yet
    if(CAN_BE_COMPILED_ON_LINUX)
        add_executable(crossgen
          ${crossgen_SOURCES}
          ${crossgen_RESOURCES}
        )
    endif(CAN_BE_COMPILED_ON_LINUX)

else()
    add_executable(crossgen
      ${crossgen_SOURCES}
      ${crossgen_RESOURCES}
    )

    target_link_libraries(crossgen
        advapi32
        ole32
        oleaut32
        uuid
        user32
        version
        shlwapi
        bcrypt
        corguids
        utilcode_crossgen
        corzap_crossgen
        jit_crossgen
        gcinfo_crossgen
        strongname_crossgen
        mdcompiler_crossgen
        mdwinmd_crossgen
        mdruntimerw_crossgen
        mdhotdata_crossgen
        mdruntime_crossgen
        cee_crossgen
        mscorlib_crossgen
        v3binder_crossgen
        ${STATIC_MT_CRT_LIB}
    )

    # Can't compile on linux yet so only add for windows
    # add the install targets
    install (TARGETS crossgen DESTINATION .)
    
    # We will generate PDB only for the debug configuration
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/crossgen.pdb DESTINATION PDB)

endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(../../zap/crossgen ../../zap/crossgen)
add_subdirectory(../../vm/crossgen ../../vm/crossgen)
add_subdirectory(../../vm/crossgen_mscorlib ../../vm/crossgen_mscorlib)
