# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.
# See the LICENSE file in the project root for more information.

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

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND SOURCES
        ../apphost.windows.cpp)

    list(APPEND HEADERS
        ../apphost.windows.h)
endif()

include(../../exe.cmake)

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
set(CORECLR_STATIC_LIB_LOCATION "$ENV{__CoreClrArtifacts}/lib")

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
else()
    set(CORECLR_LIBRARIES
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclr_static.a
        ${CORECLR_STATIC_LIB_LOCATION}/libcoreclrpal.a
        ${CORECLR_STATIC_LIB_LOCATION}/libpalrt.a
        ${CORECLR_STATIC_LIB_LOCATION}/libeventprovider.a
        ${CORECLR_STATIC_LIB_LOCATION}/libnativeresourcestring.a
    )
endif(CLR_CMAKE_TARGET_WIN32)

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
endif(CLR_CMAKE_TARGET_OSX)

# On OSX and *BSD, we use the libunwind that's part of the OS
if(CLR_CMAKE_TARGET_FREEBSD)
    find_unwind_libs(UNWIND_LIBS)

    LIST(APPEND CORECLR_LIBRARIES
      ${UNWIND_LIBS}
    )
endif(CLR_CMAKE_TARGET_FREEBSD)

if(CLR_CMAKE_TARGET_NETBSD)
    find_library(KVM kvm)

    LIST(APPEND CORECLR_LIBRARIES
      ${KVM}
    )
endif(CLR_CMAKE_TARGET_NETBSD)

if(NOT CLR_CMAKE_TARGET_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/singlefilehost_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/singlefilehost.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    set_exports_linker_option(${EXPORTS_FILE})
endif(NOT CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(singlefilehost_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(singlefilehost singlefilehost_exports)

    set_property(TARGET singlefilehost APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET singlefilehost APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

set_property(TARGET singlefilehost PROPERTY ENABLE_EXPORTS 1)

target_link_libraries(singlefilehost
    libhostfxr_static
    libhostpolicy_static
    libhostcommon
    ${CORECLR_LIBRARIES}
)
