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
remove_definitions(-DEXPORT_SHARED_API)
add_definitions(-DHOSTPOLICY_EMBEDDED)
add_definitions(-DNATIVE_LIBS_EMBEDDED)


include(../../fxr/files.cmake)
include(../../hostpolicy/files.cmake)
include(../../hostcommon/files.cmake)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../apphost.windows.cpp)

    list(APPEND HEADERS
        ../apphost.windows.h)
endif()

if(CLR_CMAKE_TARGET_WIN32)
    add_linker_flag("/DEF:${CMAKE_CURRENT_SOURCE_DIR}/singlefilehost.def")

else()
    if(CLR_CMAKE_TARGET_OSX)
        set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/singlefilehost_OSXexports.src)
    else()
        set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/singlefilehost_unixexports.src)
    endif()

    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/singlefilehost.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    set_exports_linker_option(${EXPORTS_FILE})
endif()

include(../../exe.cmake)
include(configure.cmake)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(singlefilehost_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(singlefilehost singlefilehost_exports)

    set_property(TARGET singlefilehost APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET singlefilehost APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif()

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

# Path like: artifacts/bin/coreclr/windows.x64.Release/lib  or
#            /root/runtime/artifacts/transport/coreclr/lib
set(CORECLR_STATIC_LIB_LOCATION "${CORECLR_ARTIFACTS}/lib")

message ("Looking for coreclr_static lib at location: '${CORECLR_STATIC_LIB_LOCATION}'.")

if(CLR_CMAKE_TARGET_WIN32)
    set(NATIVE_LIBS
        ${CORECLR_STATIC_LIB_LOCATION}/coreclr_static.lib
        ${CORECLR_STATIC_LIB_LOCATION}/System.Globalization.Native-Static.lib
        ${CORECLR_STATIC_LIB_LOCATION}/System.IO.Compression.Native-Static.lib
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

    set(RUNTIMEINFO_LIB ${CORECLR_STATIC_LIB_LOCATION}/runtimeinfo.lib)

else()
    set(NATIVE_LIBS
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclr_static.a
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.Globalization.Native.a
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.IO.Compression.Native.a
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.Net.Security.Native.a
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.Native.a
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.Security.Cryptography.Native.OpenSsl.a
        ${CORECLR_STATIC_LIB_LOCATION}/libpalrt.a
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclrpal.a
        ${CORECLR_STATIC_LIB_LOCATION}/libeventprovider.a
        ${CORECLR_STATIC_LIB_LOCATION}/libnativeresourcestring.a
    )

    # additional requirements for System.IO.Compression.Native
    include(${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.IO.Compression.Native/extra_libs.cmake)
    append_extra_compression_libs(NATIVE_LIBS)

    # Additional requirements for System.Net.Security.Native
    include(${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.Net.Security.Native/extra_libs.cmake)
    append_extra_security_libs(NATIVE_LIBS)

    # Additional requirements for System.Native
    include(${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.Native/extra_libs.cmake)
    append_extra_system_libs(NATIVE_LIBS)

    # Additional requirements for System.Security.Cryptography.Native.OpenSsl
    include(${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.Security.Cryptography.Native/extra_libs.cmake)
    append_extra_cryptography_libs(NATIVE_LIBS)

    set(RUNTIMEINFO_LIB ${CORECLR_STATIC_LIB_LOCATION}/libruntimeinfo.a)

endif()

if(CLR_CMAKE_TARGET_OSX)
    LIST(APPEND NATIVE_LIBS
        ${CORECLR_STATIC_LIB_LOCATION}/libSystem.Security.Cryptography.Native.Apple.a
    )

    # Additional requirements for System.Security.Cryptography.Native.Apple
    include(${CLR_REPO_ROOT_DIR}/src/libraries/Native/Unix/System.Security.Cryptography.Native.Apple/extra_libs.cmake)
    append_extra_cryptography_apple_libs(NATIVE_LIBS)
endif()

#
# Additional requirements for coreclr
#

# Specify non-default Windows libs to be used for Arm/Arm64 builds
if (CLR_CMAKE_TARGET_WIN32 AND (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64))
    LIST(APPEND NATIVE_LIBS
      Advapi32.lib
      shell32.lib
    )
endif()

if(CLR_CMAKE_TARGET_OSX)
   find_library(COREFOUNDATION CoreFoundation)
   find_library(CORESERVICES CoreServices)
   find_library(SECURITY Security)
   find_library(SYSTEM System)

   LIST(APPEND NATIVE_LIBS
     ${COREFOUNDATION}
     ${CORESERVICES}
     ${SECURITY}
     ${SYSTEM}
   )
elseif(CLR_CMAKE_TARGET_NETBSD)
     find_library(KVM kvm)

     LIST(APPEND NATIVE_LIBS
       ${KVM}
     )
elseif (CLR_CMAKE_TARGET_SUNOS)
     LIST(APPEND NATIVE_LIBS
         socket
     )
endif(CLR_CMAKE_TARGET_OSX)

# On *BSD, we always use the libunwind that's part of the OS
if(CLR_CMAKE_TARGET_FREEBSD)
  set(CLR_CMAKE_USE_SYSTEM_LIBUNWIND 1)
endif()

if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND)
    find_unwind_libs(UNWIND_LIBS)

    LIST(APPEND NATIVE_LIBS
      ${UNWIND_LIBS}
    )
endif()

if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
    # These options are used to force every object to be included even if it's unused.
    set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
    set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)
endif(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)

if(CLR_CMAKE_TARGET_OSX)
    # These options are used to force every object to be included even if it's unused.
    set(START_WHOLE_ARCHIVE -force_load)
    set(END_WHOLE_ARCHIVE )
endif(CLR_CMAKE_TARGET_OSX)

set_property(TARGET singlefilehost PROPERTY ENABLE_EXPORTS 1)

target_link_libraries(
    singlefilehost
    ${NATIVE_LIBS}

    ${START_WHOLE_ARCHIVE}
    ${RUNTIMEINFO_LIB}
    ${END_WHOLE_ARCHIVE}
)
