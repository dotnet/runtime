include(${CLR_DIR}/dac.cmake)
add_definitions(-DFEATURE_NO_HOST)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

set(CLR_DAC_SOURCES
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscordaccore.dll)

if(WIN32)
    list(APPEND CLR_DAC_SOURCES
        Native.rc
    )
    set(DEF_SOURCES
        mscordac.src
    )

    set(CURRENT_BINARY_DIR_FOR_CONFIG ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR})

    # Preprocess exports definition file
    preprocess_def_file(${CMAKE_CURRENT_SOURCE_DIR}/${DEF_SOURCES} ${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def)

    # Create target to add file dependency on mscordac.def
    add_custom_target(mscordaccore_def DEPENDS ${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def)

    # No library groups for Win32
    set(START_LIBRARY_GROUP)
    set(END_LIBRARY_GROUP)
else(WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/mscordac_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/mscordac.exports)

    # Add dependency on export file
    add_custom_target(mscordaccore_exports DEPENDS ${EXPORTS_FILE})

    if(CLR_CMAKE_PLATFORM_DARWIN OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)
        generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
    endif(CLR_CMAKE_PLATFORM_DARWIN OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)

    if(CLR_CMAKE_PLATFORM_LINUX)

        # Generate DAC export file with the DAC_ prefix
        generate_exports_file_prefix(${DEF_SOURCES} ${EXPORTS_FILE} DAC_)

        set(REDEFINES_FILE_AWK_SCRIPT ${CMAKE_SOURCE_DIR}/generateredefinesfile.awk)

        if (CLR_CMAKE_PLATFORM_ARCH_ARM OR CLR_CMAKE_PLATFORM_ARCH_ARM64)
            set(JUMP_INSTRUCTION b)
        else()
            set(JUMP_INSTRUCTION jmp)
        endif()

        # Generate the palredefines.inc file to map from the imported prefixed APIs (foo to DAC_foo)
        set(PAL_REDEFINES_INC ${GENERATED_INCLUDE_DIR}/palredefines.inc)
        add_custom_command(
            OUTPUT ${PAL_REDEFINES_INC}
            COMMAND ${AWK} -f ${REDEFINES_FILE_AWK_SCRIPT} -v jump=${JUMP_INSTRUCTION} -v prefix2=DAC_ ${DEF_SOURCES} > ${PAL_REDEFINES_INC}
            DEPENDS ${DEF_SOURCES} ${REDEFINES_FILE_AWK_SCRIPT}
            COMMENT "Generating PAL redefines file -> ${PAL_REDEFINES_INC}"
        )
        add_custom_target(pal_redefines_file DEPENDS ${PAL_REDEFINES_INC})

        # Generate the libredefines.inc file for the DAC to export the prefixed APIs (DAC_foo to foo)
        set(LIB_REDEFINES_INC ${GENERATED_INCLUDE_DIR}/libredefines.inc)
        add_custom_command(
            OUTPUT ${LIB_REDEFINES_INC}
            COMMAND ${AWK} -f ${REDEFINES_FILE_AWK_SCRIPT} -v jump=${JUMP_INSTRUCTION} -v prefix1=DAC_ ${DEF_SOURCES} > ${LIB_REDEFINES_INC}
            DEPENDS ${DEF_SOURCES} ${REDEFINES_FILE_AWK_SCRIPT}
            COMMENT "Generating DAC export redefines file -> ${LIB_REDEFINES_INC}"
        )
        add_custom_target(lib_redefines_inc DEPENDS ${LIB_REDEFINES_INC})

        # Add lib redefines file to DAC
        list(APPEND CLR_DAC_SOURCES libredefines.S)
    endif(CLR_CMAKE_PLATFORM_LINUX)
endif(WIN32)

if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)
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

    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_FREEBSD OR CLR_CMAKE_PLATFORM_NETBSD)

if(CLR_CMAKE_PLATFORM_DARWIN)
    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_DARWIN)

# Create object library to enable creation of proper dependency of mscordaccore.exp on mscordac.obj and
# mscordaccore on both the mscordaccore.exp and mscordac.obj.
_add_library(mscordacobj OBJECT mscordac.cpp)

add_library_clr(mscordaccore SHARED ${CLR_DAC_SOURCES} $<TARGET_OBJECTS:mscordacobj>)

if(CLR_CMAKE_PLATFORM_LINUX)
    add_dependencies(mscordaccore lib_redefines_inc)
endif(CLR_CMAKE_PLATFORM_LINUX)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_dependencies(mscordaccore mscordaccore_exports)
    set_property(TARGET mscordaccore APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET mscordaccore APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_PLATFORM_UNIX)

# IMPORTANT! Please do not rearrange the order of the libraries. The linker on Linux is
# order dependent and changing the order can result in undefined symbols in the shared 
# library.
set(COREDAC_LIBRARIES
    ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
    cee_dac
    cordbee_dac
    corguids
    daccess
    dbgutil
    mdcompiler_dac
    mdhotdata_dac
    mdruntime_dac
    mdruntimerw_dac
    strongname_dac
    utilcode_dac
    unwinder_dac
    ${END_LIBRARY_GROUP} # End group of libraries that have circular references
)

if(WIN32)
    # mscordac.def should be generated before mscordaccore.dll is built
    add_dependencies(mscordaccore mscordaccore_def)

    set(MSCORDAC_OBJ_PATH "${CMAKE_CURRENT_BINARY_DIR}/mscordacobj.dir/${CMAKE_CFG_INTDIR}/mscordac.obj")

    # Generate export file
    add_custom_command(
        DEPENDS mscordaccore_def "${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def" mscordacobj daccess
        OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp
        COMMAND lib.exe /OUT:"${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.lib" /DEF:"${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def" "$<TARGET_FILE:daccess>" $<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/LTCG> ${STATIC_LIBRARY_FLAGS} ${MSCORDAC_OBJ_PATH}
        COMMENT "Generating mscordaccore.exp export file"
    )

    set_source_files_properties(
        ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp
        PROPERTIES GENERATED TRUE
    )

    add_custom_target(mscordaccore_exp DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp)
    add_dependencies(mscordaccore mscordaccore_exp)

    set(COREDAC_LIBRARIES
        ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp # export file
        ${COREDAC_LIBRARIES}
        mdwinmd_dac
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
else(WIN32)
    list(APPEND COREDAC_LIBRARIES
        mscorrc_debug
        ${START_WHOLE_ARCHIVE} # force all PAL objects to be included so all exports are available  
        coreclrpal
        ${END_WHOLE_ARCHIVE}
        palrt
    )
endif(WIN32)

target_link_libraries(mscordaccore PRIVATE ${COREDAC_LIBRARIES})

# add the install targets
install_clr(mscordaccore)
