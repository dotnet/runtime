
# we need to define this library before adding .def fiels to sources.
add_library_clr(coreclr_static
    STATIC
    ${CLR_SOURCES}
)

if (CLR_CMAKE_TARGET_WIN32)
    preprocess_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    list(APPEND CLR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)
endif (CLR_CMAKE_TARGET_WIN32)

if (CLR_CMAKE_HOST_WIN32)
    set (DEF_FILE  ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)
    preprocess_file(${DEF_SOURCES} ${DEF_FILE})

    list(APPEND CLR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    # Incremental linking results in the linker inserting extra padding and routing function calls via thunks that can break the
    # invariants (e.g. size of region between Jit_PatchedCodeLast-Jit_PatchCodeStart needs to fit in a page).
    add_linker_flag("/INCREMENTAL:NO")

    # Delay load libraries required for WinRT as that is not supported on all platforms
    add_linker_flag("/DELAYLOAD:api-ms-win-core-winrt-l1-1-0.dll")

    # Delay load version.dll so that we can specify how to search when loading it as it is not part of Windows' known DLLs
    add_linker_flag("/DELAYLOAD:version.dll")

    # No library groups for Win32
    set(START_LIBRARY_GROUP)
    set(END_LIBRARY_GROUP)

else(CLR_CMAKE_HOST_WIN32)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/coreclr.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)
        # This option is necessary to ensure that the overloaded delete operator defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        add_linker_flag("-Wl,-Bsymbolic")

        # The following linked options can be inserted into the linker libraries list to
        # ensure proper resolving of circular references between a subset of the libraries.
        set(START_LIBRARY_GROUP -Wl,--start-group)
        set(END_LIBRARY_GROUP -Wl,--end-group)

        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)
    endif(CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_FREEBSD OR CLR_CMAKE_TARGET_NETBSD OR CLR_CMAKE_TARGET_SUNOS)

    if(CLR_CMAKE_TARGET_OSX)
        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -force_load)
        set(END_WHOLE_ARCHIVE )
    endif(CLR_CMAKE_TARGET_OSX)

    set_exports_linker_option(${EXPORTS_FILE})

    if(CLR_CMAKE_TARGET_ANDROID AND CLR_CMAKE_HOST_ARCH_ARM)
        set(EXPORTS_LINKER_OPTION "${EXPORTS_LINKER_OPTION} -Wl,--no-warn-shared-textrel")
    endif(CLR_CMAKE_TARGET_ANDROID AND CLR_CMAKE_HOST_ARCH_ARM)

endif (CLR_CMAKE_HOST_WIN32)

add_definitions(-DFX_VER_INTERNALNAME_STR=CoreCLR.dll)

add_library_clr(coreclr
    SHARED
    ${CLR_SOURCES}
)

add_custom_target(coreclr_exports DEPENDS ${EXPORTS_FILE})
add_custom_target(coreclr_def DEPENDS ${DEF_FILE})

add_dependencies(coreclr coreclr_def)
add_dependencies(coreclr coreclr_exports)

set_property(TARGET coreclr APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
set_property(TARGET coreclr APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})

if (CLR_CMAKE_HOST_UNIX)
    set(LIB_UNWINDER unwinder_wks)
endif (CLR_CMAKE_HOST_UNIX)

# IMPORTANT! Please do not rearrange the order of the libraries. The linker on Linux is
# order dependent and changing the order can result in undefined symbols in the shared
# library.
set(CORECLR_LIBRARIES
    utilcode
    ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
    cordbee_wks
    debug-pal
    ${LIB_UNWINDER}
    v3binder
    ${END_LIBRARY_GROUP} # End group of libraries that have circular references
    mdcompiler_wks
    mdruntime_wks
    mdruntimerw_wks
    bcltype
    ceefgen
    comfloat_wks
    corguids
    gcinfo
    utilcode
    v3binder
    System.Globalization.Native-Static
    interop
    coreclrminipal
    gc_pal
)

if(CLR_CMAKE_TARGET_WIN32)
    list(APPEND CORECLR_LIBRARIES
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
        delayimp.lib
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
elseif(CLR_CMAKE_TARGET_SUNOS)
    list(APPEND CORECLR_LIBRARIES
        socket
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

if(FEATURE_MERGE_JIT_AND_ENGINE)
    set(CLRJIT_STATIC clrjit_static)
endif(FEATURE_MERGE_JIT_AND_ENGINE)

if (CLR_CMAKE_TARGET_OSX)
    include(CMakeFindFrameworks)
    find_library(FOUNDATION Foundation REQUIRED)
endif()

target_sources(coreclr PUBLIC $<TARGET_OBJECTS:cee_wks_core>)
target_link_libraries(coreclr PUBLIC ${CORECLR_LIBRARIES} ${CLRJIT_STATIC} cee_wks ${FOUNDATION})
target_sources(coreclr_static PUBLIC $<TARGET_OBJECTS:cee_wks_core>)
target_link_libraries(coreclr_static PUBLIC ${CORECLR_LIBRARIES} clrjit_static cee_wks_mergeable ${FOUNDATION})
target_compile_definitions(coreclr_static PUBLIC CORECLR_EMBEDDED)

if(CLR_CMAKE_TARGET_WIN32)
    set(CLRDEBUGINFO_RESOURCE_PATH ${CMAKE_CURRENT_BINARY_DIR}/clr_debug_resource.bin)

    if(CLR_CMAKE_TARGET_ARCH_AMD64)
        set(CLRDEBUGINFO_RESOURCE_NAME CLRDEBUGINFOWINDOWSAMD64)
    elseif(CLR_CMAKE_TARGET_ARCH_I386)
        set(CLRDEBUGINFO_RESOURCE_NAME CLRDEBUGINFOWINDOWSX86)
    elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
        set(CLRDEBUGINFO_RESOURCE_NAME CLRDEBUGINFOWINDOWSARM64)
    endif(CLR_CMAKE_TARGET_ARCH_AMD64)

    add_custom_target(
        clr_debug_resources
        DEPENDS mscordaccore mscordbi
        # make CLRDEBUGINFO resource
        COMMAND powershell -NoProfile -ExecutionPolicy ByPass -File "${CMAKE_CURRENT_SOURCE_DIR}/GenClrDebugResource.ps1" -dac $<TARGET_FILE:mscordaccore> -dbi $<TARGET_FILE:mscordbi> -out ${CLRDEBUGINFO_RESOURCE_PATH}
    )

    configure_file(dump_helper_resource.rc.in ${CMAKE_CURRENT_BINARY_DIR}/dump_helper_resource.rc)

    set(EMBEDDED_MINIDUMP_AUXILIARY_PROVIDER ON)
    configure_file(dump_helper_resource.rc.in ${CMAKE_CURRENT_BINARY_DIR}/dump_helper_resource_embedded_minidump_provider.rc)

    target_sources(coreclr PUBLIC ${CMAKE_CURRENT_BINARY_DIR}/dump_helper_resource.rc)
    target_sources(coreclr_static PUBLIC ${CMAKE_CURRENT_BINARY_DIR}/dump_helper_resource_embedded_minidump_provider.rc)

    add_dependencies(coreclr clr_debug_resources)
    add_dependencies(coreclr_static clr_debug_resources)
endif(CLR_CMAKE_TARGET_WIN32)

# add the install targets
install_clr(TARGETS coreclr DESTINATIONS . sharedFramework COMPONENT runtime)

# Enable profile guided optimization
add_pgo(coreclr)
