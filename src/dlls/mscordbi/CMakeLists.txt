
# Set the RPATH of mscordbi so that it can find dependencies without needing to set LD_LIBRARY_PATH
# For more information: http://www.cmake.org/Wiki/CMake_RPATH_handling.
if(CORECLR_SET_RPATH)
  if(CLR_CMAKE_PLATFORM_DARWIN)
    set(CMAKE_INSTALL_RPATH "@loader_path")
  else()
    set(CMAKE_INSTALL_RPATH "\$ORIGIN")
  endif(CLR_CMAKE_PLATFORM_DARWIN)
endif(CORECLR_SET_RPATH)

set(MSCORDBI_SOURCES
  mscordbi.cpp
)  

if(WIN32)
    add_precompiled_header(stdafx.h stdafx.cpp MSCORDBI_SOURCES)
    
    add_definitions(-DFX_VER_INTERNALNAME_STR=mscordbi.dll)

    list(APPEND MSCORDBI_SOURCES
        Native.rc
    )

    set(DEF_SOURCES
      mscordbi.src
    )
    convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})

    preprocess_def_file(${DEF_SOURCES} ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)

    list(APPEND MSCORDBI_SOURCES ${CMAKE_CURRENT_BINARY_DIR}/mscordbi.def)
endif(WIN32)

add_library_clr(mscordbi SHARED
  ${MSCORDBI_SOURCES}
)

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

if(WIN32)
    list(APPEND COREDBI_LIBRARIES
        ipcmanager-staticcrt
        mdhotdata-staticcrt
        mdwinmd_dbi
        kernel32.lib
        advapi32.lib
        ole32.lib
        oleaut32.lib
        uuid.lib
        user32.lib
        version.lib
        ${STATIC_MT_CRT_LIB}
    )

    # ARM64_TODO: Enable this for Windows Arm64
    if (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
        list(APPEND COREDBI_LIBRARIES 
          ${STATIC_MT_VCRT_LIB}
        )
    endif()

    target_link_libraries(mscordbi ${COREDBI_LIBRARIES})

elseif(CLR_CMAKE_PLATFORM_UNIX)

    if(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)
        # This option is necessary to ensure that the overloaded new/delete operators defined inside
        # of the utilcode will be used instead of the standard library delete operator.
        set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} -Xlinker -Bsymbolic -Bsymbolic-functions")    
    endif(CLR_CMAKE_PLATFORM_LINUX OR CLR_CMAKE_PLATFORM_NETBSD)

    list(APPEND COREDBI_LIBRARIES
        mdhotdata_full
        # share the PAL in the dac module
        mscordaccore
    )

    # COREDBI_LIBRARIES is mentioned twice because ld is one pass linker and will not find symbols
    # if they are defined after they are used. Having all libs twice makes sure that ld will actually
    # find all symbols.
    target_link_libraries(mscordbi ${COREDBI_LIBRARIES} ${COREDBI_LIBRARIES})

    add_dependencies(mscordbi mscordaccore)
endif(WIN32)

# add the install targets
install (TARGETS mscordbi DESTINATION .)
if(WIN32)
    install (FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/mscordbi.pdb DESTINATION PDB)
endif(WIN32)
