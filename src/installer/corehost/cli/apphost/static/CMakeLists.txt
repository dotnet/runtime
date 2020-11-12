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
    ../../hostpolicy/static/coreclr_resolver.cpp
)

set(HEADERS
    ../bundle_marker.h
    ../../../hostfxr_resolver.h
)

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
add_definitions(-DEXPORT_SHARED_API=1)
add_definitions(-DHOSTPOLICY_EMBEDDED)


include(../../fxr/files.cmake)
include(../../hostpolicy/files.cmake)
include(../../hostcommon/files.cmake)

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

if(CLR_CMAKE_TARGET_WIN32)
    # Disable manifest generation into the file .exe on Windows
    add_linker_flag("/MANIFEST:NO")

    get_property(ALL_COMPILE_OPTIONS TARGET ${PROJECT_NAME} PROPERTY COMPILE_OPTIONS)
    string(TOUPPER ${RUNTIME_CONFIG} UPPERCASE_RUNTIME_CONFIG)

    # make sure that certain compiler and linker settings match the runtime config
    # we need to ensure that to be able to link with coreclr in mixed builds (ex: Debug with Release runtime)
    if(UPPERCASE_RUNTIME_CONFIG STREQUAL DEBUG OR UPPERCASE_RUNTIME_CONFIG STREQUAL CHECKED)
        # add_compile_options(/MTd)
        # per-config options will win, so we have to use this syntax to override
        set_property(TARGET ${PROJECT_NAME} PROPERTY
            COMPILE_OPTIONS "${ALL_COMPILE_OPTIONS};$<$<CONFIG:DEBUG>:/MTd>"
        )

        remove_definitions(-DNDEBUG)
        add_definitions(-DDEBUG -D_DEBUG -D_DBG)

        # ignore runtime libraries that could have been added so far based on the config
        add_linker_flag("/NODEFAULTLIB:ucrt.lib")
        add_linker_flag("/NODEFAULTLIB:libucrt.lib")
        add_linker_flag("/NODEFAULTLIB:libcmt.lib")

        # make sure we use statically linked crt
        add_linker_flag("/DEFAULTLIB:libucrtd.lib")
    else()
        # add_compile_options(/MT)
        # per-config options will win, so we have to use this syntax to override
        set_property(TARGET ${PROJECT_NAME} PROPERTY
            COMPILE_OPTIONS "${ALL_COMPILE_OPTIONS};$<$<CONFIG:DEBUG>:/MT>"
        )

        remove_definitions(-DDEBUG -D_DEBUG -D_DBG)
        add_definitions(-DNDEBUG)

        # Force uCRT to be dynamically linked for Release build
        add_linker_flag("/NODEFAULTLIB:libucrt.lib")
        add_linker_flag("/DEFAULTLIB:ucrt.lib")
    endif()

    # Incremental linking results in the linker inserting extra padding and routing function calls via thunks that can break the
    # invariants (e.g. size of region between Jit_PatchedCodeLast-Jit_PatchCodeStart needs to fit in a page).
    add_linker_flag("/INCREMENTAL:NO")
endif()

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    target_link_libraries(singlefilehost Advapi32.lib shell32.lib)
endif()

# Path like: artifacts/bin/coreclr/windows.x64.Release/lib  or
#            /root/runtime/artifacts/transport/coreclr/lib
set(CORECLR_STATIC_LIB_LOCATION "${CORECLR_ARTIFACTS}/lib")

message ("Looking for coreclr_static lib at location: '${CORECLR_STATIC_LIB_LOCATION}'.")

if(CLR_CMAKE_TARGET_WIN32)
    set(CORECLR_LIBRARIES
        ${CORECLR_STATIC_LIB_LOCATION}/coreclr_static.lib
        ${CORECLR_STATIC_LIB_LOCATION}/System.Globalization.Native.lib
        ${CORECLR_STATIC_LIB_LOCATION}/libclrcompression.lib
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
else()
    set(CORECLR_LIBRARIES
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclr_static.a
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.Globalization.Native.a
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.IO.Compression.Native.a
        ${CORECLR_STATIC_LIB_LOCATION}/libpalrt.a
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclrpal.a
        ${CORECLR_STATIC_LIB_LOCATION}/libeventprovider.a
        ${CORECLR_STATIC_LIB_LOCATION}/libnativeresourcestring.a
    )

    # additional requirements for System.IO.Compression.Native
    if (CLR_CMAKE_TARGET_BROWSER)
        add_definitions(-s USE_ZLIB)
    elseif (CLR_CMAKE_TARGET_ANDROID)
        # need special case here since we want to link against libz.so but find_package() would resolve libz.a
        set(ZLIB_LIBRARIES z)
    elseif (CLR_CMAKE_TARGET_SUNOS)
        set(ZLIB_LIBRARIES z m)
    else ()
        find_package(ZLIB REQUIRED)
    endif ()
endif()

if(CLR_CMAKE_TARGET_OSX)
   find_library(COREFOUNDATION CoreFoundation)
   find_library(CORESERVICES CoreServices)
   find_library(SECURITY Security)
   find_library(SYSTEM System)

   LIST(APPEND CORECLR_LIBRARIES
     ${COREFOUNDATION}
     ${CORESERVICES}
     ${SECURITY}
     ${SYSTEM}
   )
elseif(CLR_CMAKE_TARGET_NETBSD)
     find_library(KVM kvm)

     LIST(APPEND CORECLR_LIBRARIES
       ${KVM}
     )
elseif (CLR_CMAKE_TARGET_SUNOS)
     LIST(APPEND CORECLR_LIBRARIES
         socket
     )
endif(CLR_CMAKE_TARGET_OSX)

# On *BSD, we always use the libunwind that's part of the OS
if(CLR_CMAKE_TARGET_FREEBSD)
  set(CLR_CMAKE_USE_SYSTEM_LIBUNWIND 1)
endif()

if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
    find_unwind_libs(UNWIND_LIBS)

    LIST(APPEND CORECLR_LIBRARIES
      ${UNWIND_LIBS}
    )
endif()

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
      ${NATIVE_LIBS_LOCATION}/libSystem.Native.a
      ${NATIVE_LIBS_LOCATION}/libSystem.Net.Security.Native.a
      ${NATIVE_LIBS_LOCATION}/libSystem.Security.Cryptography.Native.OpenSsl.a
    )

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

    # Additional requirements for System.Security.Cryptography.Native.OpenSsl
    if(NOT FEATURE_DISTRO_AGNOSTIC_SSL)
        find_package(OpenSSL)
    endif()

    if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)
    endif(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
endif(NOT CLR_CMAKE_TARGET_LINUX)

set_property(TARGET singlefilehost PROPERTY ENABLE_EXPORTS 1)

target_link_libraries(singlefilehost
    ${CORECLR_LIBRARIES}

    ${START_WHOLE_ARCHIVE}
    ${NATIVE_LIBS}
    ${END_WHOLE_ARCHIVE}

    ${ZLIB_LIBRARIES}
    ${LIBGSS}
    ${NATIVE_LIBS_EXTRA}
)

if(NOT FEATURE_DISTRO_AGNOSTIC_SSL)
    target_link_libraries(singlefilehost
        ${OPENSSL_CRYPTO_LIBRARY}
        ${OPENSSL_SSL_LIBRARY}
    )
endif()
