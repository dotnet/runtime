function(clr_pgo_unknown_arch)
    if (WIN32)
        message(FATAL_ERROR "Only AMD64, ARM and I386 are supported for PGO")
    else()
        message(FATAL_ERROR "PGO not currently supported on the current platform")
    endif()
endfunction(clr_pgo_unknown_arch)

# Adds Profile Guided Optimization (PGO) flags to the current target
function(add_pgo TargetName)
    if(WIN32)
        set(ProfileFileName "${TargetName}.pgd")
    endif(WIN32)

    file(TO_NATIVE_PATH
        "${CLR_CMAKE_PACKAGES_DIR}/Microsoft.DotNet.OptimizationData.Coreclr/${CLR_CMAKE_TARGET_OS}.${CLR_CMAKE_TARGET_ARCH}/${ProfileFileName}"
        ProfilePath
    )

    # Enable PGO only for optimized configs
    set(ConfigTypeList RELEASE RELWITHDEBINFO)

    foreach(ConfigType IN LISTS ConfigTypeList)
        set(LinkFlagsProperty "LINK_FLAGS_${ConfigType}")
        if(CLR_CMAKE_PGO_INSTRUMENT)
            if(WIN32)
                set_property(TARGET ${TargetName} APPEND_STRING PROPERTY ${LinkFlagsProperty} "/LTCG /GENPROFILE")
            endif(WIN32)
        else(CLR_CMAKE_PGO_INSTRUMENT)
            # If we don't have profile data availble, gracefully fall back to a non-PGO opt build
            if(EXISTS ${ProfilePath})
                if(WIN32)
                    set_property(TARGET ${TargetName} APPEND_STRING PROPERTY ${LinkFlagsProperty} "/LTCG /USEPROFILE:PGD=${ProfilePath}")
                endif(WIN32)
            endif(EXISTS ${ProfilePath})
        endif(CLR_CMAKE_PGO_INSTRUMENT)
    endforeach(ConfigType)
endfunction(add_pgo)

if(WIN32)
  if(CLR_CMAKE_PGO_INSTRUMENT)
    # Instrumented PGO binaries on Windows introduce an additional runtime dependency, pgort<ver>.dll.
    # Make sure we copy it next to the installed product to make it easier to redistribute the package.

    string(SUBSTRING ${CMAKE_VS_PLATFORM_TOOLSET} 1 -1 VS_PLATFORM_VERSION_NUMBER)
    set(PGORT_FILENAME "pgort${VS_PLATFORM_VERSION_NUMBER}.dll")

    get_filename_component(PATH_CXX_ROOTDIR ${CMAKE_CXX_COMPILER} DIRECTORY)

    if(CLR_CMAKE_PLATFORM_ARCH_I386)
      set(PATH_VS_PGORT_DLL "${PATH_CXX_ROOTDIR}/${PGORT_FILENAME}")
    elseif(CLR_CMAKE_PLATFORM_ARCH_AMD64)
      set(PATH_VS_PGORT_DLL "${PATH_CXX_ROOTDIR}/../amd64/${PGORT_FILENAME}")
    elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
      set(PATH_VS_PGORT_DLL "${PATH_CXX_ROOTDIR}/../arm/${PGORT_FILENAME}")
    else()
      clr_pgo_unknown_arch()
    endif()

    if (EXISTS ${PATH_VS_PGORT_DLL})
      message(STATUS "Found PGO runtime: ${PATH_VS_PGORT_DLL}")
      install(PROGRAMS ${PATH_VS_PGORT_DLL} DESTINATION .)
    else()
      message(FATAL_ERROR "file not found: ${PATH_VS_PGORT_DLL}")
    endif()

  endif(CLR_CMAKE_PGO_INSTRUMENT)
endif(WIN32)
