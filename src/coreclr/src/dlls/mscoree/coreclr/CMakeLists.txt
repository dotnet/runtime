if (WIN32)
    preprocess_def_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    list(APPEND CLR_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/coreclr.def)

    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /ENTRY:CoreDllMain")

    # No library groups for Win32
    set(START_LIBRARY_GROUP)
    set(END_LIBRARY_GROUP)

else()
    add_definitions(-DNO_CRT_INIT)

    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/coreclr.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})

    if(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
        # This option is necessary to ensure that the overloaded delete operator defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")

        # The following linked options can be inserted into the linker libraries list to 
        # ensure proper resolving of circular references between a subset of the libraries.
        set(START_LIBRARY_GROUP -Wl,--start-group)
        set(END_LIBRARY_GROUP -Wl,--end-group)

        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive) 

        set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
    endif(CMAKE_SYSTEM_NAME STREQUAL Linux OR CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

    if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -force_load)
        set(END_WHOLE_ARCHIVE )

        set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
    endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

endif (WIN32)

add_definitions(-DFX_VER_INTERNALNAME_STR=CoreCLR.dll)

add_library(coreclr
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
    ClrJit
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
        mdwinmd_wks
        comnls_wks
        comcrypt_wks
        msvcrt.lib
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        shlwapi.lib
        bcrypt.lib
        crypt32.lib
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

target_link_libraries(coreclr ${CORECLR_LIBRARIES})

if(WIN32)

    add_dependencies(coreclr dactablegen)

    # Add dac table & debug resource to coreclr
    get_include_directories(INC_DIR)
    get_compile_definitions(PREPROCESS_DEFINITIONS)
    list(APPEND INC_DIR -I${CLR_DIR}/src/vm -I${CLR_DIR}/src/vm/amd64 -I${CLR_DIR}/src/debug/ee -I${CLR_DIR}/src/gc)
    list(APPEND PREPROCESS_DEFINITIONS -DDACCESS_COMPILE -DDBG_TARGET_64BIT=1 -DDBG_TARGET_WIN64=1 -DDBG_TARGET_AMD64=1)
    add_custom_command(TARGET coreclr
        POST_BUILD
        COMMAND ${CMAKE_CXX_COMPILER} /P /EP /TP ${PREPROCESS_DEFINITIONS} ${INC_DIR} /Fi${CMAKE_CURRENT_BINARY_DIR}/daccess.i  ${CLR_DIR}/src/debug/daccess/daccess.cpp
        COMMAND $<TARGET_FILE:dactablegen_exe> /dac:${CMAKE_CURRENT_BINARY_DIR}/daccess.i /pdb:${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/coreclr.pdb /dll:$<TARGET_FILE:coreclr> /bin:${CMAKE_CURRENT_BINARY_DIR}/wks.bin
        COMMAND $<TARGET_FILE:InjectResource> /bin:${CMAKE_CURRENT_BINARY_DIR}/wks.bin /dll:$<TARGET_FILE:coreclr>
        COMMAND $<TARGET_FILE:GenClrDebugResource> /dac:$<TARGET_FILE:mscordaccore> /dbi:$<TARGET_FILE:mscordbi> /sku:onecoreclr /out:${CMAKE_CURRENT_BINARY_DIR}/clrDebugResource.bin
        COMMAND $<TARGET_FILE:InjectResource> /bin:${CMAKE_CURRENT_BINARY_DIR}/clrDebugResource.bin /dll:$<TARGET_FILE:coreclr> /name:CLRDEBUGINFO
        COMMENT Add dactable & debug resources to coreclr
    )
endif(WIN32)

# add the install targets
install (TARGETS coreclr DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/coreclr.pdb DESTINATION PDB)
endif(WIN32)

