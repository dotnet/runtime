# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(singlefilehost)
set(DOTNET_PROJECT_NAME "singlefilehost")

# Add RPATH to the apphost binary that allows using local copies of shared libraries
# dotnet core depends on for special scenarios when system wide installation of such
# dependencies is not possible for some reason.
# This cannot be enabled for MacOS (Darwin) since its RPATH works in a different way,
# doesn't apply to libraries loaded via dlopen and most importantly, it is not transitive.
if (NOT CLR_CMAKE_TARGET_OSX)
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "\$ORIGIN/netcoredeps")
endif()

set(SKIP_VERSIONING 1)

include_directories(..)
include_directories(../../json)

set(SOURCES
    ../bundle_marker.cpp
    ./hostfxr_resolver.cpp
    ./hostpolicy_resolver.cpp
)

set(HEADERS
    ../bundle_marker.h
    ../../../hostfxr_resolver.h
)

list(APPEND SOURCES $<TARGET_OBJECTS:libhostfxr_static> $<TARGET_OBJECTS:libhostpolicy_static>)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../apphost.windows.cpp)

    list(APPEND HEADERS
        ../apphost.windows.h)
endif()

include(../../exe.cmake)
include(configure.cmake)

add_definitions(-DFEATURE_APPHOST=1)
add_definitions(-DFEATURE_STATIC_HOST=1)

# Disable manifest generation into the file .exe on Windows
if(CLR_CMAKE_TARGET_WIN32)
    set_property(TARGET ${PROJECT_NAME} PROPERTY
            LINK_FLAGS "/MANIFEST:NO"
        )
endif()

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    target_link_libraries(singlefilehost Advapi32.lib shell32.lib)
endif()

# Path like: artifacts/bin/coreclr/Windows_NT.x64.Release/lib  or
#            /root/runtime/artifacts/transport/coreclr/lib
set(CORECLR_STATIC_LIB_LOCATION "${CORECLR_ARTIFACTS}/lib")

message ("Looking for coreclr_static lib at location: '${CORECLR_STATIC_LIB_LOCATION}'.")

if(CLR_CMAKE_TARGET_WIN32)
    set(CORECLR_LIBRARIES
        # Disable superhost on Win32 for now.
        # ${CORECLR_STATIC_LIB_LOCATION}/coreclr_static.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        shlwapi.lib
        bcrypt.lib
        RuntimeObject.lib
    )
elseif(CLR_CMAKE_TARGET_LINUX)
    set(CORECLR_LIBRARIES
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclr_static.a
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclrpal.a
        ${CORECLR_STATIC_LIB_LOCATION}/libpalrt.a
        ${CORECLR_STATIC_LIB_LOCATION}/libeventprovider.a
        ${CORECLR_STATIC_LIB_LOCATION}/libnativeresourcestring.a
    )

    # currently linking coreclr into the singlefilehost is only supported on linux
    # the following code here would be needed if/when BSD and OSX are supported too
    #
    # if(CLR_CMAKE_TARGET_OSX)
    #   find_library(COREFOUNDATION CoreFoundation)
    #   find_library(CORESERVICES CoreServices)
    #   find_library(SECURITY Security)
    #   find_library(SYSTEM System)
    # 
    #   LIST(APPEND CORECLR_LIBRARIES
    #     ${COREFOUNDATION}
    #     ${CORESERVICES}
    #     ${SECURITY}
    #     ${SYSTEM}
    #   )
    # endif(CLR_CMAKE_TARGET_OSX)
    # 
    # # On OSX and *BSD, we use the libunwind that's part of the OS
    # if(CLR_CMAKE_TARGET_FREEBSD)
    #     find_unwind_libs(UNWIND_LIBS)
    # 
    #     LIST(APPEND CORECLR_LIBRARIES
    #       ${UNWIND_LIBS}
    #     )
    # endif(CLR_CMAKE_TARGET_FREEBSD)
    # 
    # if(CLR_CMAKE_TARGET_NETBSD)
    #     find_library(KVM kvm)
    # 
    #     LIST(APPEND CORECLR_LIBRARIES
    #       ${KVM}
    #     )
    # endif(CLR_CMAKE_TARGET_NETBSD)
endif(CLR_CMAKE_TARGET_WIN32)

# Path like: artifacts/bin/native/net5.0-Linux-Release-arm/
set(NATIVE_LIBS_LOCATION "${NATIVE_LIBS_ARTIFACTS}")
message ("Looking for native libs at location: '${NATIVE_LIBS_LOCATION}'.")

if(NOT CLR_CMAKE_TARGET_LINUX)
    set(NATIVE_LIBS
        # Native libs linked into singlefilehost is supported only on Linux for now.
        # if/when BSD and OSX are supported too, consider the commented code sections below.
    )
else()
    set(NATIVE_LIBS
      ${NATIVE_LIBS_LOCATION}/libSystem.IO.Compression.Native.a
      ${NATIVE_LIBS_LOCATION}/libSystem.Native.a
      ${NATIVE_LIBS_LOCATION}/libSystem.Net.Security.Native.a
      ${NATIVE_LIBS_LOCATION}/libSystem.Security.Cryptography.Native.OpenSsl.a
    )

    find_package(ZLIB REQUIRED)

    # Additional requirements for System.System.IO.Compression.Native
    #
    # if (CLR_CMAKE_TARGET_SUNOS)
    #     set(ZLIB_LIBRARIES z m)
    # elseif (CLR_CMAKE_TARGET_UNIX)
    #     find_package(ZLIB REQUIRED)
    # endif ()
    
    # Additional requirements for System.Net.Security.Native
    if (HAVE_GSSFW_HEADERS)
       find_library(LIBGSS NAMES GSS)
       if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
         message(FATAL_ERROR "Cannot find GSS.Framework and System.Net.Security.Native cannot build without it. Try installing GSS.Framework (or the appropriate package for your platform)")
       endif()
    elseif(HAVE_HEIMDAL_HEADERS)
       find_library(LIBGSS NAMES gssapi)
       if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
         message(FATAL_ERROR "Cannot find libgssapi and System.Net.Security.Native cannot build without it. Try installing heimdal (or the appropriate package for your platform)")
       endif()
    else()
       find_library(LIBGSS NAMES gssapi_krb5)
       if(LIBGSS STREQUAL LIBGSS-NOTFOUND)
         message(FATAL_ERROR "Cannot find libgssapi_krb5 and System.Net.Security.Native cannot build without it. Try installing libkrb5-dev (or the appropriate package for your platform)")
       endif()
    endif()
    
    # Additional requirements for System.Native
    if (CLR_CMAKE_TARGET_LINUX AND NOT CLR_CMAKE_TARGET_ANDROID)
        set(NATIVE_LIBS_EXTRA
          rt
        )
    # elseif (CLR_CMAKE_TARGET_FREEBSD)
    #     set(NATIVE_LIBS_EXTRA
    #       pthread
    #     )
    #     find_library(INOTIFY_LIBRARY inotify HINTS /usr/local/lib)
    #     if(NOT (INOTIFY_LIBRARY STREQUAL INOTIFY_LIBRARY-NOTFOUND))
    #       LIST(APPEND NATIVE_LIBS_EXTRA
    #         ${INOTIFY_LIBRARY}
    #       )
    #     endif ()
    # elseif (CLR_CMAKE_TARGET_SUNOS)
    #     set(NATIVE_LIBS_EXTRA
    #         socket
    #     )
    endif ()

    if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)
    endif(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
endif(NOT CLR_CMAKE_TARGET_LINUX)

set_property(TARGET singlefilehost PROPERTY ENABLE_EXPORTS 1)

target_link_libraries(singlefilehost
    libhostcommon
    ${CORECLR_LIBRARIES}

    ${ZLIB_LIBRARIES}
    ${LIBGSS}
    ${NATIVE_LIBS_EXTRA}

    ${START_WHOLE_ARCHIVE}
    ${NATIVE_LIBS}
    ${END_WHOLE_ARCHIVE}
)
