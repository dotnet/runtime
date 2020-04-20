
# Set the RPATH of mscordbi so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if(CORECLR_SET_RPATH)
  set(CMAKE_BUILD_WITH_INSTALL_RPATH TRUE)
  if(CLR_CMAKE_HOST_OSX)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_HOST_OSX)
endif(CORECLR_SET_RPATH)

set(MSCORDBI_SOURCES
    mscordbi.cpp
)

# Add the DAC PAL export mapping file
if(CLR_CMAKE_HOST_LINUX)
    list(APPEND MSCORDBI_SOURCES ${PAL_REDEFINES_FILE})
endif(CLR_CMAKE_HOST_LINUX)

if(CLR_CMAKE_HOST_WIN32)
    add_definitions(-DFX_VER_INTERNALNAME_STR=mscordbi.dll)

    list(APPEND MSCORDBI_SOURCES
        Native.rc
    )

    set(DEF_SOURCES
        mscordbi.src
    )

    convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})

    preprocess_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)

    list(APPEND MSCORDBI_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)
else(CLR_CMAKE_HOST_WIN32)
    set(DEF_SOURCES ${CMAKE_CURRENT_SOURCE_DIR}/mscordbi_unixexports.src)
    set(EXPORTS_FILE ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.exports)
    generate_exports_file(${DEF_SOURCES} ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_WIN32)

if(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD)
    # This option is necessary to ensure that the overloaded new/delete operators defined inside
    # of the utilcode will be used instead of the standard library delete operator.
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic")

    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,--version-script=${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD)

if(CLR_CMAKE_HOST_OSX)
    # Add linker exports file option
    set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_OSX)

add_library_clr(mscordbi SHARED ${MSCORDBI_SOURCES})
target_precompile_header(TARGET mscordbi HEADER stdafx.h)

if(CLR_CMAKE_HOST_UNIX)
    add_custom_target(mscordbi_exports DEPENDS ${EXPORTS_FILE})
    add_dependencies(mscordbi mscordbi_exports)

    set_property(TARGET mscordbi APPEND_STRING PROPERTY LINK_FLAGS ${EXPORTS_LINKER_OPTION})
    set_property(TARGET mscordbi APPEND_STRING PROPERTY LINK_DEPENDS ${EXPORTS_FILE})
endif(CLR_CMAKE_HOST_UNIX)

set(COREDBI_LIBRARIES
    debug-pal
    cordbdi
    utilcodestaticnohost
    ildbsymlib
    mdcompiler-dbi
    mdruntime-dbi
    mdruntimerw-dbi
    mddatasource_dbi
    corguids
)

if(CLR_CMAKE_HOST_WIN32)
    if(CLR_CMAKE_TARGET_WIN32)
        set(COREDBI_TARGET_WIN32_LIBRARIES mdwinmd_dbi)
    endif(CLR_CMAKE_TARGET_WIN32)

    list(APPEND COREDBI_LIBRARIES
        mdhotdata-staticcrt
        ${COREDBI_TARGET_WIN32_LIBRARIES}
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

    target_link_libraries(mscordbi ${COREDBI_LIBRARIES})

elseif(CLR_CMAKE_HOST_UNIX)

    list(APPEND COREDBI_LIBRARIES
        mdhotdata_full
        palrt
        # share the PAL in the dac module
        mscordaccore
    )

    # COREDBI_LIBRARIES is mentioned twice because ld is one pass linker and will not find symbols
    # if they are defined after they are used. Having all libs twice makes sure that ld will actually
    # find all symbols.
    target_link_libraries(mscordbi ${COREDBI_LIBRARIES} ${COREDBI_LIBRARIES})

    add_dependencies(mscordbi mscordaccore)

    if(CLR_CMAKE_HOST_LINUX)
        add_dependencies(mscordbi pal_redefines_file)
    endif(CLR_CMAKE_HOST_LINUX)

endif(CLR_CMAKE_HOST_WIN32)

# Create the DBI module index header file containing the DBI build id
# for xplat and the timestamp/size on Windows.
if(FEATURE_SINGLE_FILE_DIAGNOSTICS)
    set(
        DBI_MODULE_INDEX_FILE 
        ${GENERATED_INCLUDE_DIR}/dbimoduleindex.h
    )
    add_custom_command(
        OUTPUT ${DBI_MODULE_INDEX_FILE}
        COMMAND ${CLR_DOTNET_COMMAND} ${CMAKE_INSTALL_PREFIX}/GetModuleIndex/GetModuleIndex.dll $<TARGET_FILE:mscordbi> ${DBI_MODULE_INDEX_FILE}
        DEPENDS mscordbi
        COMMENT "Generating DBI module index file -> ${DBI_MODULE_INDEX_FILE}"
    )
    set_source_files_properties(
        ${DBI_MODULE_INDEX_FILE} 
        PROPERTIES GENERATED TRUE
    )
    add_custom_target(
        dbi_module_index_header
        DEPENDS ${DBI_MODULE_INDEX_FILE} 
    )
endif(FEATURE_SINGLE_FILE_DIAGNOSTICS)

# add the install targets
install_clr(TARGETS mscordbi ADDITIONAL_DESTINATION sharedFramework)
