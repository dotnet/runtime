project(createdump)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(BEFORE ${VM_DIR})
if(CLR_CMAKE_HOST_OSX)
  include_directories(${CLR_DIR}/inc/llvm)
endif(CLR_CMAKE_HOST_OSX)

remove_definitions(-DUNICODE)
remove_definitions(-D_UNICODE)

if(CLR_CMAKE_HOST_WIN32)

    set(CREATEDUMP_SOURCES
        main.cpp
        dumpname.cpp
        createdumpwindows.cpp
        createdump.rc
    )

    add_executable_clr(createdump
        ${CREATEDUMP_SOURCES}
    )

    target_link_libraries(createdump
        kernel32.lib
        ${STATIC_MT_CRT_LIB}
        advapi32.lib
        version.lib
        dbghelp.lib
        ws2_32.lib
    )

else(CLR_CMAKE_HOST_WIN32)

    include(configure.cmake)

    # Set the RPATH of createdump so that it can find dependencies without needing to set LD_LIBRARY_PATH
    # For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
    if (CORECLR_SET_RPATH)
      set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
      if(CLR_CMAKE_HOST_OSX)
        set(CMAKE_INSTALL_RPATH "@loader_path")
      else()
        set(CMAKE_INSTALL_RPATH "\$ORIGIN")
      endif(CLR_CMAKE_HOST_OSX)
    endif (CORECLR_SET_RPATH)

    add_definitions(-DPAL_STDCPP_COMPAT)

    # This is so we can include "version.c"
    include_directories(${CMAKE_BINARY_DIR})

    set(CREATEDUMP_SOURCES
        main.cpp
        dumpname.cpp
        createdumpunix.cpp
        crashinfo.cpp
        threadinfo.cpp
        datatarget.cpp
        dumpwriter.cpp
        crashreportwriter.cpp
    )

if(CLR_CMAKE_HOST_OSX)
    add_executable_clr(createdump
        crashinfomac.cpp
        threadinfomac.cpp
        dumpwritermacho.cpp
        ${CREATEDUMP_SOURCES}
    )
else()
    add_executable_clr(createdump
        crashinfounix.cpp
        threadinfounix.cpp
        dumpwriterelf.cpp
        ${CREATEDUMP_SOURCES}
        ${PAL_REDEFINES_FILE}
    )
    add_dependencies(createdump pal_redefines_file)
endif(CLR_CMAKE_HOST_OSX)

    target_link_libraries(createdump
        corguids
        dbgutil
        # share the PAL in the dac module
        mscordaccore
        dl
    )

    add_dependencies(createdump mscordaccore)

endif(CLR_CMAKE_HOST_WIN32)

install_clr(TARGETS createdump DESTINATIONS . sharedFramework COMPONENT runtime)
