
# mscordbi_universal is a parallel build of the DBI that owns its own PAL instead
# of sharing the DAC's PAL.

# Set the RPATH of mscordbi_universal so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if(CORECLR_SET_RPATH)
  if(CLR_CMAKE_HOST_OSX)
    set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
    set(CMAKE_INSTALL_NAME_DIR "@rpath")
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_HOST_OSX)
endif(CORECLR_SET_RPATH)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

# Reuse the DBI entry-point source and precompiled header from the sibling mscordbi
# directory so the two binaries stay in sync.
set(MSCORDBI_DIR ${CMAKE_CURRENT_SOURCE_DIR}/../mscordbi)
include_directories(${MSCORDBI_DIR})

set(MSCORDBI_UNIVERSAL_SOURCES
    ${MSCORDBI_DIR}/mscordbi.cpp
)

# Reuse the sibling precompiled header and the shared DBI entry-point source, but keep a
# local export list: the universal binary omits the HMODULE-based OpenVirtualProcess
# entrypoints (see mscordbi_universal.src / mscordbi_universal_unixexports.src).

# Unlike mscordbi, the DAC PAL export redefines file (palredefines.S) is deliberately
# not linked here: this binary provides its own PAL rather than importing the DAC's.

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DFX_VER_INTERNALNAME_STR=mscordbi_universal.dll)

    list(APPEND MSCORDBI_UNIVERSAL_SOURCES
        ${MSCORDBI_DIR}/Native.rc
    )

    set(DEF_SOURCES
        mscordbi_universal.src
    )

    convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})

    preprocess_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordbi_universal.def)

    list(APPEND MSCORDBI_UNIVERSAL_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/mscordbi_universal.def)
else(CLR_CMAKE_HOST_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/mscordbi_universal_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/mscordbi_universal.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS OR CLR_CMAKE_HOST_HAIKU)
        # This option is necessary to ensure that the overloaded new/delete operators defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        add_link_options(LINKER:-Bsymbolic)
    endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS OR CLR_CMAKE_HOST_HAIKU)

    set_exports_linker_option(${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_WIN32)

add_library_clr(mscordbi_universal SHARED ${MSCORDBI_UNIVERSAL_SOURCES})
set_target_properties(mscordbi_universal PROPERTIES DBI_COMPONENT TRUE)
# This binary links its own PAL, so the shared mscordbi.cpp must not also define the
# PAL-owned g_arm64_atomics_present symbol (see mscordbi.cpp).
target_compile_definitions(mscordbi_universal PRIVATE MSCORDBI_LINKS_PRIVATE_PAL)
target_precompile_headers(mscordbi_universal PRIVATE $<$<COMPILE_LANGUAGE:CXX>:stdafx.h>)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(mscordbi_universal_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(mscordbi_universal mscordbi_universal_exports)

    set_property(TARGET mscordbi_universal APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET mscordbi_universal APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

set(COREDBI_LIBRARIES
    debug-pal
    cordbdi
    dbgutil
    utilcodestaticnohost
    mdcompiler-dbi
    mdruntime-dbi
    mdruntimerw-dbi
    mddatasource_dbi
    corguids
    minipal
)

if(CLR_CMAKE_HOST_WIN32)

    list(APPEND COREDBI_LIBRARIES
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )

    target_link_libraries(mscordbi_universal PRIVATE ${COREDBI_LIBRARIES})

elseif(CLR_CMAKE_HOST_UNIX)

    list(APPEND COREDBI_LIBRARIES
        # Link a private PAL rather than sharing the DAC's PAL through mscordaccore.
        # This lets DBI load the DAC (via OpenVirtualProcessImpl2's path-based flow)
        # into its own PAL without cross-PAL HMODULE use.
        mscorrc
        coreclrpal
        coreclrminipal
    )

    if(CLR_CMAKE_HOST_HAIKU)
        list(APPEND COREDBI_LIBRARIES
            network
        )
    endif(CLR_CMAKE_HOST_HAIKU)

    # Before llvm 16, lld was setting `--undefined-version` by default. The default was
    # flipped to `--no-undefined-version` in lld 16, so we will explicitly set it to
    # `--undefined-version` for our use-case.
    include(CheckLinkerFlag OPTIONAL)
    if(COMMAND check_linker_flag)
        check_linker_flag(CXX -Wl,--undefined-version LINKER_SUPPORTS_UNDEFINED_VERSION)
        if (LINKER_SUPPORTS_UNDEFINED_VERSION)
            add_linker_flag(-Wl,--undefined-version)
        endif(LINKER_SUPPORTS_UNDEFINED_VERSION)
    endif(COMMAND check_linker_flag)

    # COREDBI_LIBRARIES is mentioned twice because ld is one pass linker and will not find symbols
    # if they are defined after they are used. Having all libs twice makes sure that ld will actually
    # find all symbols.
    target_link_libraries(mscordbi_universal PRIVATE ${COREDBI_LIBRARIES} ${COREDBI_LIBRARIES})

endif(CLR_CMAKE_HOST_WIN32)

esrp_sign(mscordbi_universal)

# Install only to the non-framework output (CoreCLRArtifactsPath). The universal DBI
# is opt-in and must not ship in the shared framework / runtime pack, mirroring the
# mscordaccore_universal install destination.
install_clr(TARGETS mscordbi_universal DESTINATIONS . COMPONENT debug)
