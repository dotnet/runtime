if (WIN32)
    preprocess_def_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    list(APPEND CLR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /ENTRY:CoreDllMain")

    # Incremental linking results in the linker inserting extra padding and routing function calls via thunks that can break the
    # invariants (e.g. size of region between Jit_PatchedCodeLast-Jit_PatchCodeStart needs to fit in a page).
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /INCREMENTAL:NO")

    # Delay load libraries required for WinRT as that is not supported on all platforms
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DELAYLOAD:api-ms-win-core-winrt-string-l1-1-0.dll")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DELAYLOAD:api-ms-win-core-winrt-l1-1-0.dll")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DELAYLOAD:api-ms-win-core-winrt-roparameterizediid-l1-1-0.dll")
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DELAYLOAD:api-ms-win-ro-typeresolution-l1-1-0.dll")

    # Delay load version.dll so that we can specify how to search when loading it as it is not part of Windows' known DLLs
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DELAYLOAD:version.dll")

    # No library groups for Win32
    set(START_LIBRARY_GROUP)
    set(END_LIBRARY_GROUP)

else()
    add_definitions(-DNO_CRT_INIT)

    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/coreclr.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD OR CMAKE_SYSTEM_NAME STREQUAL NetBSD)
        # This option is necessary to ensure that the overloaded delete operator defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Xlinker -Bsymbolic-functions")

        # The following linked options can be inserted into the linker libraries list to
        # ensure proper resolving of circular references between a subset of the libraries.
        set(START_LIBRARY_GROUP -Wl,--start-group)
        set(END_LIBRARY_GROUP -Wl,--end-group)

        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)

        set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
    endif(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD OR CMAKE_SYSTEM_NAME STREQUAL NetBSD)

    if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -force_load)
        set(END_WHOLE_ARCHIVE )

        set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
    endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

endif (WIN32)

add_definitions(-DFX_VER_INTERNALNAME_STR=CoreCLR.dll)

add_library_clr(coreclr
    SHARED
    ${CLR_SOURCES}
)

add_custom_target(coreclr_exports DEPENDS ${EXPORTS_FILE})
add_dependencies(coreclr coreclr_exports)

set_property(TARGET coreclr APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
set_property(TARGET coreclr APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})

if (CLR_CMAKE_PLATFORM_UNIX)
    set(LIB_UNWINDER unwinder_wks)
endif (CLR_CMAKE_PLATFORM_UNIX)

if(FEATURE_MERGE_JIT_AND_ENGINE)
    set(CLRJIT_STATIC clrjit_static)
endif(FEATURE_MERGE_JIT_AND_ENGINE)

# IMPORTANT! Please do not rearrange the order of the libraries. The linker on Linux is
# order dependent and changing the order can result in undefined symbols in the shared
# library.
set(CORECLR_LIBRARIES
    utilcode
    ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
    cordbee_wks
    debug-pal
    ${LIB_UNWINDER}
    cee_wks
    ${END_LIBRARY_GROUP} # End group of libraries that have circular references
    mdcompiler_wks
    mdruntime_wks
    mdruntimerw_wks
    mdhotdata_full
    bcltype
    ceefgen
    ${CLRJIT_STATIC}
    comfloat_wks
    corguids
    gcinfo # Condition="'$(TargetCpu)'=='amd64' or '$(TargetCpu)' == 'arm' or '$(TargetCpu)' == 'arm64'"
    ildbsymlib
    strongname_wks
    utilcode
    v3binder
)

if(WIN32)
    list(APPEND CORECLR_LIBRARIES
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
        mdwinmd_wks
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
    list(APPEND CORECLR_LIBRARIES
        ${START_WHOLE_ARCHIVE} # force all PAL objects to be included so all exports are available
        coreclrpal
        ${END_WHOLE_ARCHIVE}
        mscorrc_debug
        palrt
    )
endif(WIN32)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
    list(APPEND CORECLR_LIBRARIES
        tracepointprovider
    )
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(FEATURE_PERFTRACING)
    list(APPEND CORECLR_LIBRARIES
        eventpipe
    )
endif(FEATURE_PERFTRACING)

if(FEATURE_EVENT_TRACE)
    if(CLR_CMAKE_PLATFORM_UNIX)
        list(APPEND CORECLR_LIBRARIES
            eventprovider # On Windows this library contains only macros
        )
    endif(CLR_CMAKE_PLATFORM_UNIX)
endif(FEATURE_EVENT_TRACE)

target_link_libraries(coreclr ${CORECLR_LIBRARIES})

if(WIN32)
    # Add dac table & debug resource to coreclr
    get_include_directories(INC_DIR)
    get_compile_definitions(PREPROCESS_DEFINITIONS)
    list(APPEND INC_DIR -I${CLR_DIR}/src/vm -I${CLR_DIR}/src/vm/${ARCH_SOURCES_DIR} -I${CLR_DIR}/src/debug/ee -I${CLR_DIR}/src/gc)
    list(APPEND PREPROCESS_DEFINITIONS -DDACCESS_COMPILE -DDBG_TARGET_64BIT=1 -DDBG_TARGET_WIN64=1)

    if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
        list(APPEND PREPROCESS_DEFINITIONS -DDBG_TARGET_AMD64=1)
    elseif (CLR_CMAKE_PLATFORM_ARCH_ARM64)
        list(APPEND PREPROCESS_DEFINITIONS -DDBG_TARGET_ARM64=1)
    elseif (CLR_CMAKE_PLATFORM_ARCH_ARM)
        list(APPEND PREPROCESS_DEFINITIONS -DDBG_TARGET_ARM=1)
    elseif (CLR_CMAKE_PLATFORM_ARCH_I386)
        list(APPEND PREPROCESS_DEFINITIONS -DDBG_TARGET_X86=1)
    else()
        clr_unknown_arch()
    endif()

    set(DACTABLEGEN_EXE ${CMAKE_BINARY_DIR}/src/ToolBox/SOS/DacTableGen/dactablegen.exe)

    if (CLR_CMAKE_CROSS_ARCH)
      include(${CMAKE_INSTALL_PREFIX}/${CLR_CMAKE_CROSS_HOST_ARCH}/dactabletools/dactabletools.cmake)
    endif()

    set(INJECT_RESOURCE_EXE $<TARGET_FILE:InjectResource>)
    set(GEN_CLR_DEBUG_RESOURCE_EXE $<TARGET_FILE:GenClrDebugResource>)

    add_custom_command(
        DEPENDS coreclr mscordaccore mscordbi ${CLR_DIR}/src/debug/daccess/daccess.cpp
        OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/inject_debug_resources.timestamp
        COMMAND ${CMAKE_CXX_COMPILER} /P /EP /TP ${PREPROCESS_DEFINITIONS} ${INC_DIR} /Fi${CMAKE_CURRENT_BINARY_DIR}/daccess.i  ${CLR_DIR}/src/debug/daccess/daccess.cpp
        COMMAND ${DACTABLEGEN_EXE} /dac:${CMAKE_CURRENT_BINARY_DIR}/daccess.i /pdb:${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/coreclr.pdb /dll:$<TARGET_FILE:coreclr> /bin:${CMAKE_CURRENT_BINARY_DIR}/wks.bin
        COMMAND ${INJECT_RESOURCE_EXE} /bin:${CMAKE_CURRENT_BINARY_DIR}/wks.bin /dll:$<TARGET_FILE:coreclr>
        COMMAND ${GEN_CLR_DEBUG_RESOURCE_EXE} /dac:$<TARGET_FILE:mscordaccore> /dbi:$<TARGET_FILE:mscordbi> /sku:onecoreclr /out:${CMAKE_CURRENT_BINARY_DIR}/clrDebugResource.bin
        COMMAND ${INJECT_RESOURCE_EXE} /bin:${CMAKE_CURRENT_BINARY_DIR}/clrDebugResource.bin /dll:$<TARGET_FILE:coreclr> /name:CLRDEBUGINFO
        COMMAND ${INJECT_RESOURCE_EXE} /bin:${CMAKE_CURRENT_SOURCE_DIR}/dump_helper_resource.bin /dll:$<TARGET_FILE:coreclr> /name:MINIDUMP_AUXILIARY_PROVIDER
        COMMAND ${CMAKE_COMMAND} -E touch ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/inject_debug_resources.timestamp
        COMMENT Add dactable, debug resources, and dump helper resources to coreclr
    )

    if(NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
        add_custom_target(inject_debug_resources ALL DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/inject_debug_resources.timestamp)
    endif()

endif(WIN32)

# add the install targets
install_clr(coreclr)

# Enable profile guided optimization
add_pgo(coreclr)
