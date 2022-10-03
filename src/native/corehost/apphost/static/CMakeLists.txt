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
include_directories(${CLR_SRC_NATIVE_DIR}/libs/System.IO.Compression.Native)
include_directories(${CLR_SRC_NATIVE_DIR}/libs/Common)

set(SOURCES
    ../bundle_marker.cpp
    ./hostfxr_resolver.cpp
    ./hostpolicy_resolver.cpp
    ../../hostpolicy/static/coreclr_resolver.cpp
)

set(HEADERS
    ../bundle_marker.h
    ../../hostfxr_resolver.h
)

add_definitions(-D_NO_ASYNCRTIMP)
add_definitions(-D_NO_PPLXIMP)
remove_definitions(-DEXPORT_SHARED_API)
add_definitions(-DHOSTPOLICY_EMBEDDED)
add_definitions(-DNATIVE_LIBS_EMBEDDED)


include(../../fxr/files.cmake)
include(../../hostpolicy/files.cmake)
include(../../hostcommon/files.cmake)

if(MSVC)
    # Host components don't try to handle asynchronous exceptions
    add_compile_options(/EHsc)
elseif (CMAKE_CXX_COMPILER_ID MATCHES GNU)
    # Prevents libc from calling pthread_cond_destroy on static objects in
    # dlopen()'ed library which we dlclose() in pal::unload_library.
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-fno-use-cxa-atexit>)
endif()

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

if (CLR_SINGLE_FILE_HOST_ONLY)
    set(ADDITIONAL_INSTALL_ARGUMENTS COMPONENT runtime)
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

    # Incremental linking results in the linker inserting extra padding and routing function calls via thunks that can break the
    # invariants (e.g. size of region between Jit_PatchedCodeLast-Jit_PatchCodeStart needs to fit in a page).
    add_linker_flag("/INCREMENTAL:NO")

    # Delay load libraries required for WinRT as that is not supported on all platforms
    add_linker_flag("/DELAYLOAD:api-ms-win-core-winrt-l1-1-0.dll")
endif()

if(CLR_CMAKE_TARGET_WIN32)
    set(NATIVE_LIBS
        coreclr_static

        System.Globalization.Native-Static
        System.IO.Compression.Native-Static

        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        shlwapi.lib
        shell32.lib
        bcrypt.lib
        RuntimeObject.lib
        delayimp.lib
    )

    set(RUNTIMEINFO_LIB runtimeinfo)

else()
    set(NATIVE_LIBS
        coreclr_static

        System.Globalization.Native-Static
        System.IO.Compression.Native-Static
        System.Net.Security.Native-Static
        System.Native-Static
        System.Security.Cryptography.Native.OpenSsl-Static

        palrt
        coreclrpal
        eventprovider
        nativeresourcestring
    )

    # additional requirements for System.IO.Compression.Native
    include(${CLR_SRC_NATIVE_DIR}/libs/System.IO.Compression.Native/extra_libs.cmake)
    append_extra_compression_libs(NATIVE_LIBS)

    # Additional requirements for System.Net.Security.Native
    include(${CLR_SRC_NATIVE_DIR}/libs/System.Net.Security.Native/extra_libs.cmake)
    append_extra_security_libs(NATIVE_LIBS)

    # Additional requirements for System.Native
    include(${CLR_SRC_NATIVE_DIR}/libs/System.Native/extra_libs.cmake)
    append_extra_system_libs(NATIVE_LIBS)

    # Additional requirements for System.Security.Cryptography.Native.OpenSsl
    include(${CLR_SRC_NATIVE_DIR}/libs/System.Security.Cryptography.Native/extra_libs.cmake)
    append_extra_cryptography_libs(NATIVE_LIBS)

    set(RUNTIMEINFO_LIB runtimeinfo)

endif()

if(CLR_CMAKE_TARGET_OSX)
    LIST(APPEND NATIVE_LIBS
        System.Security.Cryptography.Native.Apple-Static
    )

    # Additional requirements for System.Security.Cryptography.Native.Apple
    include(${CLR_SRC_NATIVE_DIR}/libs/System.Security.Cryptography.Native.Apple/extra_libs.cmake)
    append_extra_cryptography_apple_libs(NATIVE_LIBS)
endif()

#
# Additional requirements for coreclr
#
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

if(CLR_CMAKE_USE_SYSTEM_LIBUNWIND AND NOT CLR_CMAKE_TARGET_OSX)
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
