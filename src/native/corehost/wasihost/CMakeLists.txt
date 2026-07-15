# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(wasihost)
set(DOTNET_PROJECT_NAME "WasiHost-Static")

# HAVE_DIRENT_D_TYPE / HAVE_GETAUXVAL for the shared corerun pal header.
include(configure.cmake)

set(WASIHOST_SOURCES
    ./wasihost.cpp
)

# Reuse the corerun pal header (path/CORE_ROOT/TPA helpers); it's header-only, so no corerun object
# is linked here.
set(WASIHOST_CORERUN_DIR ${CLR_REPO_ROOT_DIR}/src/coreclr/hosts/corerun)

add_library(WasiHost-Static
    STATIC
    ${WASIHOST_SOURCES}
)
set_target_properties(WasiHost-Static PROPERTIES OUTPUT_NAME WasiHost CLEAN_DIRECT_OUTPUT 1)

# The coreclr build sets these wasi emulation defines globally; the corehost build does not. The
# matching -lwasi-emulated-* libraries are supplied at the per-app relink.
if (CLR_CMAKE_TARGET_WASI)
    target_compile_definitions(WasiHost-Static PRIVATE
        _WASI_EMULATED_MMAN
        _WASI_EMULATED_GETPID
        _WASI_EMULATED_SIGNAL
        _WASI_EMULATED_PROCESS_CLOCKS)
endif()

target_include_directories(WasiHost-Static PRIVATE
    ${CMAKE_CURRENT_BINARY_DIR}  # generated config.h
    ${WASIHOST_CORERUN_DIR}      # corerun.hpp (shared pal)
    ${CLR_SRC_NATIVE_DIR}        # minipal/*.h
)

# The runtime static libraries and the app-generated callhelpers are supplied at the per-app relink
# (src/mono/wasi/build/WasiApp.CoreCLR.targets), which pulls this archive whole-archive for main().
install(TARGETS WasiHost-Static DESTINATION sharedFramework COMPONENT runtime)
