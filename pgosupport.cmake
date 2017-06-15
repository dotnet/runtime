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
    else(WIN32)
        set(ProfileFileName "${TargetName}.profdata")
    endif(WIN32)

    set(CLR_CMAKE_OPTDATA_PACKAGEWITHRID "optimization.${CLR_CMAKE_TARGET_OS}-${CLR_CMAKE_TARGET_ARCH}.PGO.CoreCLR")

    # On case-sensitive file systems, NuGet packages are restored to lowercase paths
    string(TOLOWER "${CLR_CMAKE_OPTDATA_PACKAGEWITHRID}/${CLR_CMAKE_OPTDATA_VERSION}" OptDataVersionedSubPath)

    file(TO_NATIVE_PATH
        "${CLR_CMAKE_PACKAGES_DIR}/${OptDataVersionedSubPath}/data/${ProfileFileName}"
        ProfilePath
    )

    if(CLR_CMAKE_PGO_INSTRUMENT)
        if(WIN32)
            set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELEASE        " /LTCG /GENPROFILE")
            set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELWITHDEBINFO " /LTCG /GENPROFILE")
        else(WIN32)
            if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
                target_compile_options(${TargetName} PRIVATE -flto -fprofile-instr-generate)
                set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS " -flto -fuse-ld=gold -fprofile-instr-generate")
            endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
        endif(WIN32)
    else(CLR_CMAKE_PGO_INSTRUMENT)
        # If we don't have profile data availble, gracefully fall back to a non-PGO opt build
        if(EXISTS ${ProfilePath})
            if(WIN32)
                set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELEASE        " /LTCG /USEPROFILE:PGD=${ProfilePath}")
                set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELWITHDEBINFO " /LTCG /USEPROFILE:PGD=${ProfilePath}")
            else(WIN32)
                if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
                    if(NOT CMAKE_CXX_COMPILER_VERSION VERSION_LESS 3.6)
                        if(HAVE_LTO)
                            target_compile_options(${TargetName} PRIVATE -flto -fprofile-instr-use=${ProfilePath} -Wno-profile-instr-out-of-date)
                            set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS " -flto -fuse-ld=gold -fprofile-instr-use=${ProfilePath}")
                        else(HAVE_LTO)
                            message(WARNING "LTO is not supported, skipping profile guided optimizations")
                        endif(HAVE_LTO)
                    else(NOT CMAKE_CXX_COMPILER_VERSION VERSION_LESS 3.6)
                        message(WARNING "PGO is not supported; Clang 3.6 or later is required for profile guided optimizations")
                    endif(NOT CMAKE_CXX_COMPILER_VERSION VERSION_LESS 3.6)
                endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
            endif(WIN32)
        endif(EXISTS ${ProfilePath})
    endif(CLR_CMAKE_PGO_INSTRUMENT)
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
