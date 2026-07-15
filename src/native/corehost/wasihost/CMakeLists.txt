# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

project(wasihost)
set(DOTNET_PROJECT_NAME "WasiHost-Static")

# HAVE_DIRENT_D_TYPE / HAVE_GETAUXVAL for the shared corerun pal header.
include(configure.cmake)

set(WASIHOST_SOURCES
    ./wasihost.cpp
)

# Reuse the corerun pal (path handling, CORE_ROOT/TPA directory enumeration, absolute-path
# resolution) so the host's assembly discovery stays identical to the validated corerun-based host.
# The header is self-contained (only <config.h> and minipal headers), so no corerun runtime object
# is linked into this archive.
set(WASIHOST_CORERUN_DIR ${CLR_REPO_ROOT_DIR}/src/coreclr/hosts/corerun)

add_library(WasiHost-Static
    STATIC
    ${WASIHOST_SOURCES}
)
set_target_properties(WasiHost-Static PROPERTIES OUTPUT_NAME WasiHost CLEAN_DIRECT_OUTPUT 1)

# The shared corerun pal header compiles the wasi paths against the emulated wasi-libc facilities
# (mmap/getpid/signal/process-clocks). The coreclr build sets these globally for wasi; the corehost
# build does not, so set them here. The matching -lwasi-emulated-* libraries are supplied at the
# per-app relink (src/mono/wasi/build/WasiApp.CoreCLR.targets).
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

# The runtime static libraries (coreclr_static, System.Native, minipal, ...) and the app-generated
# callhelpers are NOT linked here; they are supplied at the per-app relink from the runtime pack
# (src/mono/wasi/build/WasiApp.CoreCLR.targets), which pulls this archive whole-archive so
# main()/_start are included.
install(TARGETS WasiHost-Static DESTINATION sharedFramework COMPONENT runtime)
