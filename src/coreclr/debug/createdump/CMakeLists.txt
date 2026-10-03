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
        createdumpmain.cpp
        shared/createdumpcore.cpp
        shared/dumpname.cpp
        createdumpwindows.cpp
        createdump.rc
    )

    add_executable_clr(createdump
        ${CREATEDUMP_SOURCES}
    )

    target_link_libraries(createdump
        PRIVATE
        kernel32.lib
        ${STATIC_MT_CRT_LIB}
        advapi32.lib
        version.lib
        dbghelp.lib
        ws2_32.lib
        ntdll.lib
    )

else(CLR_CMAKE_HOST_WIN32)

    if(NOT DEFINED ENV{ROOTFS_DIR})
      include_directories(SYSTEM /usr/local/include)
    elseif (CLR_CMAKE_TARGET_FREEBSD)
      include_directories(SYSTEM $ENV{ROOTFS_DIR}/usr/local/include)
    endif()

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

    # This is so we can include "version.c"
    include_directories(${CMAKE_BINARY_DIR})

    set(CREATEDUMP_SHARED_SOURCES
        shared/createdumpcore.cpp
        shared/dumpname.cpp
        shared/crashinfocore.cpp
        shared/dumpwriter.cpp
    )

    set(CREATEDUMP_SOURCES
        createdumpmain.cpp
        createdumpunix.cpp
        crashinfo.cpp
        threadinfo.cpp
        datatarget.cpp
        crashreportwriter.cpp
        ${CREATEDUMP_SHARED_SOURCES}
    )

if(CLR_CMAKE_HOST_OSX)
    add_library_clr(createdump_static
        STATIC
        crashinfomac.cpp
        threadinfomac.cpp
        shared/dumpwritermacho.cpp
        ${CREATEDUMP_SOURCES}
    )
else()
    set(CREATEDUMP_SHARED_LINUX_SOURCES
        shared/crashinfounixcore.cpp
        shared/createdumpunixcore.cpp
        shared/dumpwriterelf.cpp
        shared/threadsnapshot.cpp
    )

    add_library_clr(createdump_static
        STATIC
        crashinfounix.cpp
        threadinfounix.cpp
        ${CREATEDUMP_SHARED_LINUX_SOURCES}
        ${CREATEDUMP_SOURCES}
    )
endif(CLR_CMAKE_HOST_OSX)
target_link_libraries(createdump_static PRIVATE minipal)

    add_executable_clr(createdump
        main.cpp
        createdumppal.cpp
    )

    target_link_libraries(createdump
        PRIVATE
        createdump_static
        corguids
        coreclrpal_dac
        dbgutil
        ${CMAKE_DL_LIBS}
    )

endif(CLR_CMAKE_HOST_WIN32)

target_link_libraries(createdump PRIVATE minipal)

if (CLR_CMAKE_HOST_APPLE)
    adhoc_sign_with_entitlements(createdump "${CLR_ENG_NATIVE_DIR}/createdump-entitlements.plist")
endif()

install_clr(TARGETS createdump DESTINATIONS . sharedFramework COMPONENT runtime)

if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_OSX)
    install_clr(TARGETS createdump DESTINATIONS . COMPONENT nativeaot)
endif()
