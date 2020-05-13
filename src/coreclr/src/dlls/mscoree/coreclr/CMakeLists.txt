if (CLR_CMAKE_TARGET_WIN32)
    preprocess_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    list(APPEND CLR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)
endif (CLR_CMAKE_TARGET_WIN32)

if (CLR_CMAKE_HOST_WIN32)
    preprocess_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    list(APPEND CLR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    add_link_options(/ENTRY:CoreDllMain)

    # Incremental linking results in the linker inserting extra padding and routing function calls via thunks that can break the
    # invariants (e.g. size of region between Jit_PatchedCodeLast-Jit_PatchCodeStart needs to fit in a page).
    add_link_options(/INCREMENTAL:NO)

    # Delay load libraries required for WinRT as that is not supported on all platforms
    add_link_options("/DELAYLOAD:api-ms-win-core-winrt-string-l1-1-0.dll")
    add_link_options("/DELAYLOAD:api-ms-win-core-winrt-l1-1-0.dll")
    add_link_options("/DELAYLOAD:api-ms-win-core-winrt-roparameterizediid-l1-1-0.dll")
    add_link_options("/DELAYLOAD:api-ms-win-ro-typeresolution-l1-1-0.dll")

    # Delay load version.dll so that we can specify how to search when loading it as it is not part of Windows' known DLLs
    add_link_options("/DELAYLOAD:version.dll")

    # No library groups for Win32
    set(START_LIBRARY_GROUP)
    set(END_LIBRARY_GROUP)

else(CLR_CMAKE_HOST_WIN32)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/coreclr.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD)
        # This option is necessary to ensure that the overloaded delete operator defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        add_link_options("LINKER:-Bsymbolic")

        # The following linked options can be inserted into the linker libraries list to
        # ensure proper resolving of circular references between a subset of the libraries.
        set(START_LIBRARY_GROUP -Wl,--start-group)
        set(END_LIBRARY_GROUP -Wl,--end-group)

        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)

        set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
    endif(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD)

    if(CLR_CMAKE_TARGET_OSX)
        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -force_load)
        set(END_WHOLE_ARCHIVE )

        set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
    endif(CLR_CMAKE_TARGET_OSX)

    if(CLR_CMAKE_TARGET_ANDROID AND CLR_CMAKE_HOST_ARCH_ARM)
        set(EXPORTS_LINKER_OPTION "${EXPORTS_LINKER_OPTION} -Wl,--no-warn-shared-textrel")
    endif(CLR_CMAKE_TARGET_ANDROID AND CLR_CMAKE_HOST_ARCH_ARM)

    if(CLR_CMAKE_TARGET_SUNOS)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,-M,${EXPORTS_FILE})
    endif(CLR_CMAKE_TARGET_SUNOS)

endif (CLR_CMAKE_HOST_WIN32)

add_definitions(-DFX_VER_INTERNALNAME_STR=CoreCLR.dll)

add_library_clr(coreclr
    SHARED
    ${CLR_SOURCES}
)

add_custom_target(coreclr_exports DEPENDS ${EXPORTS_FILE})
add_dependencies(coreclr coreclr_exports)

set_property(TARGET coreclr APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
set_property(TARGET coreclr APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})

if (CLR_CMAKE_HOST_UNIX)
    set(LIB_UNWINDER unwinder_wks)
endif (CLR_CMAKE_HOST_UNIX)

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
    v3binder
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
    gcinfo
    ildbsymlib
    utilcode
    v3binder
    libraries-native
    System.Globalization.Native-Static
    interop
)

if(CLR_CMAKE_TARGET_WIN32)
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
        mscorrc
        palrt
    )
endif(CLR_CMAKE_TARGET_WIN32)

if(CLR_CMAKE_TARGET_LINUX)
    list(APPEND CORECLR_LIBRARIES
        ${START_WHOLE_ARCHIVE}
        tracepointprovider
        ${END_WHOLE_ARCHIVE}
    )
endif(CLR_CMAKE_TARGET_LINUX)

if(FEATURE_PERFTRACING)
    list(APPEND CORECLR_LIBRARIES
        eventpipe
    )
endif(FEATURE_PERFTRACING)

if(FEATURE_EVENT_TRACE)
    if(CLR_CMAKE_HOST_UNIX)
        list(APPEND CORECLR_LIBRARIES
            eventprovider # On Windows this library contains only macros
        )
    endif(CLR_CMAKE_HOST_UNIX)
endif(FEATURE_EVENT_TRACE)

target_link_libraries(coreclr ${CORECLR_LIBRARIES})

# Create the runtime module index header file containing the coreclr build id
# for xplat and the timestamp/size on Windows.
if(FEATURE_SINGLE_FILE_DIAGNOSTICS)
    generate_module_index(coreclr ${GENERATED_INCLUDE_DIR}/runtimemoduleindex.h)
endif(FEATURE_SINGLE_FILE_DIAGNOSTICS)

if(CLR_CMAKE_TARGET_WIN32)
    if (NOT CMAKE_GENERATOR MATCHES "Visual Studio .*")
      add_custom_target(inject_debug_resources ALL COMMAND UNABLE_TO_EMBED_DAC_ON_WINDOWS_NON_VS_GENERATOR)
      add_dependencies(inject_debug_resources coreclr mscordaccore mscordbi)
    else()
      # Add dac table & debug resource to coreclr
      get_include_directories(INC_DIR)
      get_compile_definitions(PREPROCESS_DEFINITIONS)
      list(APPEND INC_DIR -I${CLR_DIR}/src/vm -I${CLR_DIR}/src/vm/${ARCH_SOURCES_DIR} -I${CLR_DIR}/src/debug/ee -I${CLR_DIR}/src/gc)
      list(APPEND PREPROCESS_DEFINITIONS -DDACCESS_COMPILE -DTARGET_64BIT)

      if (CLR_CMAKE_HOST_ARCH_AMD64)
          list(APPEND PREPROCESS_DEFINITIONS -DTARGET_AMD64)
      elseif (CLR_CMAKE_HOST_ARCH_ARM64)
          list(APPEND PREPROCESS_DEFINITIONS -DTARGET_ARM64)
      elseif (CLR_CMAKE_HOST_ARCH_ARM)
          list(APPEND PREPROCESS_DEFINITIONS -DTARGET_ARM)
      elseif (CLR_CMAKE_HOST_ARCH_I386)
          list(APPEND PREPROCESS_DEFINITIONS -DTARGET_X86)
      else()
          clr_unknown_arch()
      endif()


      if (CLR_CMAKE_CROSS_ARCH)
        include(${CMAKE_INSTALL_PREFIX}/${CLR_CMAKE_CROSS_HOST_ARCH}/dactabletools/dactabletools.cmake)
      endif()

      add_custom_command(
          DEPENDS coreclr mscordaccore mscordbi ${CLR_DIR}/src/debug/daccess/daccess.cpp
          OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/inject_debug_resources.timestamp
          COMMAND ${CMAKE_CXX_COMPILER} /P /EP /TP ${PREPROCESS_DEFINITIONS} ${INC_DIR} /Fi${CMAKE_CURRENT_BINARY_DIR}/daccess.i  ${CLR_DIR}/src/debug/daccess/daccess.cpp
          COMMAND dactablegen /dac:${CMAKE_CURRENT_BINARY_DIR}/daccess.i /pdb:${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/coreclr.pdb /dll:$<TARGET_FILE:coreclr> /bin:${CMAKE_CURRENT_BINARY_DIR}/wks.bin
          COMMAND InjectResource /bin:${CMAKE_CURRENT_BINARY_DIR}/wks.bin /dll:$<TARGET_FILE:coreclr>
          COMMAND GenClrDebugResource /dac:$<TARGET_FILE:mscordaccore> /dbi:$<TARGET_FILE:mscordbi> /sku:onecoreclr /out:${CMAKE_CURRENT_BINARY_DIR}/clrDebugResource.bin
          COMMAND InjectResource /bin:${CMAKE_CURRENT_BINARY_DIR}/clrDebugResource.bin /dll:$<TARGET_FILE:coreclr> /name:CLRDEBUGINFO
          COMMAND InjectResource /bin:${CMAKE_CURRENT_SOURCE_DIR}/dump_helper_resource.bin /dll:$<TARGET_FILE:coreclr> /name:MINIDUMP_AUXILIARY_PROVIDER
          COMMAND ${CMAKE_COMMAND} -E touch ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/inject_debug_resources.timestamp
          COMMENT Add dactable, debug resources, and dump helper resources to coreclr
      )

      if(NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
          add_custom_target(inject_debug_resources ALL DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/inject_debug_resources.timestamp)
      endif()
    endif()

endif(CLR_CMAKE_TARGET_WIN32)

# add the install targets
install_clr(TARGETS coreclr ADDITIONAL_DESTINATION sharedFramework)

# Enable profile guided optimization
add_pgo(coreclr)
