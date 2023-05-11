add_definitions(-DFEATURE_NO_HOST)

set(CLR_DAC_SOURCES
)

add_definitions(-DFX_VER_INTERNALNAME_STR=mscordaccore.dll)

if(CLR_CMAKE_HOST_WIN32)
    list(APPEND CLR_DAC_SOURCES
        Native.rc
    )
    set(DEF_SOURCES
        mscordac.src
    )

    set(CURRENT_BINARY_DIR_FOR_CONFIG ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR})

    # Preprocess exports definition file
    preprocess_file(${CMAKE_CURRENT_SOURCE_DIR}/${DEF_SOURCES} ${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def)

    # Create target to add file dependency on mscordac.def
    add_custom_target(mscordaccore_def DEPENDS ${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def)

    # No library groups for Win32
    set(START_LIBRARY_GROUP)
    set(END_LIBRARY_GROUP)
else(CLR_CMAKE_HOST_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/mscordac_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/mscordac.exports)

    # Add dependency on export file
    add_custom_target(mscordaccore_exports DEPENDS ${EXPORTS_FILE})

    if(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
        generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
    endif(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)

    if(CORECLR_SET_RPATH AND CLR_CMAKE_HOST_OSX AND CLR_CMAKE_HOST_ARCH_ARM64)
      set(CMAKE_BUILD_WITH_INSTALL_NAME_DIR ON)
      set(CMAKE_INSTALL_NAME_DIR "@rpath")
    endif(CORECLR_SET_RPATH AND CLR_CMAKE_HOST_OSX AND CLR_CMAKE_HOST_ARCH_ARM64)

    if(CLR_CMAKE_HOST_LINUX)

        # Generate DAC export file with the DAC_ prefix
        generate_exports_file_prefix(${DEF_SOURCES} ${EXPORTS_FILE} DAC_)

        set(REDEFINES_FILE_SCRIPT ${CMAKE_SOURCE_DIR}/generateredefinesfile.sh)

        if (CLR_CMAKE_HOST_ARCH_ARM OR CLR_CMAKE_HOST_ARCH_ARM64 OR CLR_CMAKE_HOST_ARCH_LOONGARCH64)
            set(JUMP_INSTRUCTION b)
        elseif (CLR_CMAKE_HOST_ARCH_RISCV64)
            set(JUMP_INSTRUCTION tail)
        else()
            set(JUMP_INSTRUCTION jmp)
        endif()

        # Generate the palredefines.inc file to map from the imported prefixed APIs (foo to DAC_foo)
        set(PAL_REDEFINES_INC ${GENERATED_INCLUDE_DIR}/palredefines.inc)
        add_custom_command(
            OUTPUT ${PAL_REDEFINES_INC}
            COMMAND ${REDEFINES_FILE_SCRIPT} ${DEF_SOURCES} ${JUMP_INSTRUCTION} "" DAC_ > ${PAL_REDEFINES_INC}
            DEPENDS ${DEF_SOURCES} ${REDEFINES_FILE_SCRIPT}
            COMMENT "Generating PAL redefines file -> ${PAL_REDEFINES_INC}"
            VERBATIM
        )
        add_custom_target(pal_redefines_file DEPENDS ${PAL_REDEFINES_INC})

        # Generate the libredefines.inc file for the DAC to export the prefixed APIs (DAC_foo to foo)
        set(LIB_REDEFINES_INC ${GENERATED_INCLUDE_DIR}/libredefines.inc)
        add_custom_command(
            OUTPUT ${LIB_REDEFINES_INC}
            COMMAND ${REDEFINES_FILE_SCRIPT} ${DEF_SOURCES} ${JUMP_INSTRUCTION} DAC_ > ${LIB_REDEFINES_INC}
            DEPENDS ${DEF_SOURCES} ${REDEFINES_FILE_SCRIPT}
            COMMENT "Generating DAC export redefines file -> ${LIB_REDEFINES_INC}"
        )
        add_custom_target(lib_redefines_inc DEPENDS ${LIB_REDEFINES_INC})

        # Add lib redefines file to DAC
        list(APPEND CLR_DAC_SOURCES libredefines.S)
    endif(CLR_CMAKE_HOST_LINUX)

    if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
        # This option is necessary to ensure that the overloaded delete operator defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic")

        # The following linked options can be inserted into the linker libraries list to
        # ensure proper resolving of circular references between a subset of the libraries.
        set(START_LIBRARY_GROUP -Wl,--start-group)
        set(END_LIBRARY_GROUP -Wl,--end-group)

        # These options are used to force every object to be included even if it's unused.
        set(START_WHOLE_ARCHIVE -Wl,--whole-archive)
        set(END_WHOLE_ARCHIVE -Wl,--no-whole-archive)
    endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)

    set_exports_linker_option(${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_WIN32)

# Create object library to enable creation of proper dependency of mscordaccore.exp on mscordac.obj and
# mscordaccore on both the mscordaccore.exp and mscordac.obj.
add_library_clr(mscordacobj OBJECT mscordac.cpp)

add_library_clr(mscordaccore SHARED ${CLR_DAC_SOURCES} $<TARGET_OBJECTS:mscordacobj>)

set_target_properties(mscordacobj mscordaccore PROPERTIES DAC_COMPONENT TRUE)

if(CLR_CMAKE_HOST_LINUX)
    add_dependencies(mscordaccore lib_redefines_inc)
endif(CLR_CMAKE_HOST_LINUX)

if(CLR_CMAKE_HOST_UNIX)
    add_dependencies(mscordaccore mscordaccore_exports)
    set_property(TARGET mscordaccore APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET mscordaccore APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

# IMPORTANT! Please do not rearrange the order of the libraries. The linker on Linux is
# order dependent and changing the order can result in undefined symbols in the shared
# library.
set(COREDAC_LIBRARIES
    ${START_LIBRARY_GROUP} # Start group of libraries that have circular references
    cee_dac
    cordbee_dac
    ${START_WHOLE_ARCHIVE} # force all exports to be available
    corguids
    daccess
    ${END_WHOLE_ARCHIVE}
    dbgutil
    mdcompiler_dac
    mdruntime_dac
    mdruntimerw_dac
    utilcode_dac
    unwinder_dac
    ${END_LIBRARY_GROUP} # End group of libraries that have circular references
)

if(CLR_CMAKE_HOST_WIN32)
    # mscordac.def should be generated before mscordaccore.dll is built
    add_dependencies(mscordaccore mscordaccore_def)

    # Generate export file
    add_custom_command(
        DEPENDS mscordaccore_def "${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def" mscordacobj daccess
        OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp
        COMMAND lib.exe /OUT:"${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.lib" /DEF:"${CURRENT_BINARY_DIR_FOR_CONFIG}/mscordac.def" "$<TARGET_FILE:daccess>" $<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/LTCG> ${STATIC_LIBRARY_FLAGS} $<TARGET_OBJECTS:mscordacobj>
        COMMENT "Generating mscordaccore.exp export file"
    )

    set_source_files_properties(
        ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp
        PROPERTIES GENERATED TRUE
    )

    add_custom_target(mscordaccore_exp DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp)
    add_dependencies(mscordaccore_exp mscordacobj)
    add_dependencies(mscordaccore mscordaccore_exp mscordacobj)

    set(COREDAC_LIBRARIES
        ${CMAKE_CURRENT_BINARY_DIR}/mscordaccore.exp # export file
        ${COREDAC_LIBRARIES}
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        coreclrminipal
        ${STATIC_MT_CRT_LIB}
        ${STATIC_MT_VCRT_LIB}
    )
else(CLR_CMAKE_HOST_WIN32)
    list(APPEND COREDAC_LIBRARIES
        mscorrc
        ${START_WHOLE_ARCHIVE} # force all PAL objects to be included so all exports are available
        coreclrpal
        palrt
        coreclrminipal
        ${END_WHOLE_ARCHIVE}
    )
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_WIN32 AND CLR_CMAKE_TARGET_UNIX)
    list(APPEND COREDAC_LIBRARIES
        libunwind_xdac
    )
endif(CLR_CMAKE_HOST_WIN32 AND CLR_CMAKE_TARGET_UNIX)

if(CLR_CMAKE_HOST_UNIX)
    list(APPEND COREDAC_LIBRARIES
        coreclrpal_dac
    )
endif(CLR_CMAKE_HOST_UNIX)

target_link_libraries(mscordaccore PRIVATE ${COREDAC_LIBRARIES})

# add the install targets
install_clr(TARGETS mscordaccore DESTINATIONS . sharedFramework COMPONENT runtime)

if(CLR_CMAKE_HOST_WIN32)
    set(LONG_NAME_HOST_ARCH ${CLR_CMAKE_HOST_ARCH})
    set(LONG_NAME_TARGET_ARCH ${CLR_CMAKE_TARGET_ARCH})
    if (LONG_NAME_HOST_ARCH STREQUAL x64)
        set(LONG_NAME_HOST_ARCH "amd64")
    endif()
    if (LONG_NAME_TARGET_ARCH STREQUAL x64)
        set(LONG_NAME_TARGET_ARCH "amd64")
    endif()
    message ("Read file version from native version header at '${VERSION_HEADER_PATH}'.")
    file(READ "${VERSION_HEADER_PATH}" NATIVE_VERSION_HEADER)
    string(REGEX MATCH "#define VER_FILEVERSION[ \t]+[0-9]+(,[0-9]+)+" FILE_VERSION_LINE "${NATIVE_VERSION_HEADER}")
    string(REGEX MATCHALL "[0-9]+" FILE_VERSION_COMPONENTS "${FILE_VERSION_LINE}")
    list(JOIN FILE_VERSION_COMPONENTS "." FILE_VERSION)
    install(PROGRAMS $<TARGET_FILE:mscordaccore> RENAME mscordaccore_${LONG_NAME_HOST_ARCH}_${LONG_NAME_TARGET_ARCH}_${FILE_VERSION}.dll DESTINATION sharedFramework COMPONENT runtime)
    if (NOT FEATURE_CROSSBITNESS)
        install(PROGRAMS $<TARGET_FILE:mscordaccore> RENAME mscordaccore_${LONG_NAME_HOST_ARCH}_${LONG_NAME_TARGET_ARCH}_${FILE_VERSION}.dll DESTINATION sharedFramework COMPONENT crosscomponents)
    endif()
endif()
