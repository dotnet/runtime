include(CheckCXXSourceCompiles)
include(CheckCXXCompilerFlag)

# VC++ guarantees support for LTCG (LTO's equivalent)
if(NOT WIN32)
  # Function required to give CMAKE_REQUIRED_* local scope
  function(check_have_lto_and_pgodata_supported profile_path)
    set(CMAKE_REQUIRED_FLAGS "-flto -fprofile-instr-use=${profile_path} -Wno-profile-instr-out-of-date -Wno-profile-instr-unprofiled")
    set(CMAKE_REQUIRED_LIBRARIES -flto)
    check_cxx_source_compiles("int main() { return 0; }" HAVE_LTO_AND_PGO_DATA_SUPPORTED)
  endfunction(check_have_lto_and_pgodata_supported)

  check_cxx_compiler_flag(-faligned-new COMPILER_SUPPORTS_F_ALIGNED_NEW)
endif(NOT WIN32)

# Adds Profile Guided Optimization (PGO) flags to the current target
function(add_pgo TargetName)
    if(CLR_CMAKE_PGO_INSTRUMENT)
        if(CLR_CMAKE_HOST_WIN32)
            set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELEASE        " /LTCG /GENPROFILE")
            set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELWITHDEBINFO " /LTCG /GENPROFILE")
        else(CLR_CMAKE_HOST_WIN32)
            if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
                target_compile_options(${TargetName} PRIVATE -flto -fprofile-instr-generate)
                set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS " -flto -fprofile-instr-generate")
            endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
        endif(CLR_CMAKE_HOST_WIN32)
    elseif(CLR_CMAKE_PGO_OPTIMIZE AND NOT CLR_CMAKE_ENABLE_SANITIZERS)
        if(CLR_CMAKE_HOST_WIN32)
            set(ProfileFileName "${TargetName}.pgd")
        else(CLR_CMAKE_HOST_WIN32)
            set(ProfileFileName "coreclr.profdata")
        endif(CLR_CMAKE_HOST_WIN32)

        file(TO_NATIVE_PATH
            "${CLR_CMAKE_OPTDATA_PATH}/data/${ProfileFileName}"
            ProfilePath
        )

        # If we don't have profile data available, gracefully fall back to a non-PGO opt build
        if(NOT EXISTS ${ProfilePath})
            message("PGO data file NOT found: ${ProfilePath}")
        elseif(CMAKE_GENERATOR MATCHES "Visual Studio")
            # MSVC is sensitive to exactly the options passed during PGO optimization and Ninja and
            # MSBuild differ slightly (but not meaningfully for runtime behavior)
            message("Cannot use PGO optimization built with Ninja from MSBuild. Re-run build with Ninja to apply PGO information")
        else(NOT EXISTS ${ProfilePath})
            if(CLR_CMAKE_HOST_WIN32)
                set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELEASE        " /LTCG /USEPROFILE:PGD=\"${ProfilePath}\"")
                set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS_RELWITHDEBINFO " /LTCG /USEPROFILE:PGD=\"${ProfilePath}\"")
                add_compile_definitions(WITH_NATIVE_PGO)
            else(CLR_CMAKE_HOST_WIN32)
                if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
                    if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
                        check_have_lto_and_pgodata_supported(${ProfilePath})
                        if(HAVE_LTO_AND_PGO_DATA_SUPPORTED)
                            message(STATUS "Enabling profile guided optimizations for ${TargetName}")
                            target_compile_options(${TargetName} PRIVATE -flto -fprofile-instr-use=${ProfilePath} -Wno-profile-instr-out-of-date -Wno-profile-instr-unprofiled)
                            set_property(TARGET ${TargetName} APPEND_STRING PROPERTY LINK_FLAGS " -flto -fprofile-instr-use=${ProfilePath}")
                            add_compile_definitions(WITH_NATIVE_PGO)
                        else(HAVE_LTO_AND_PGO_DATA_SUPPORTED)
                            message(WARNING "LTO is not supported or PGO optimization data not compatible, skipping profile guided optimizations for ${TargetName}")
                        endif(HAVE_LTO_AND_PGO_DATA_SUPPORTED)
                    endif(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
                endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
            endif(CLR_CMAKE_HOST_WIN32)
        endif(NOT EXISTS ${ProfilePath})
    endif(CLR_CMAKE_PGO_INSTRUMENT)
endfunction(add_pgo)

# On non-Windows platforms, PGO compile flags (-fprofile-instr-generate for instrumentation,
# -fprofile-instr-use for optimization) must be applied at directory scope so they also affect
# the static and object libraries (cee_wks, utilcode, coreclrpal, etc.) that are linked into
# the final shared libraries. The per-target add_pgo function above only applies compile flags
# to the shared library target itself, which only covers its directly compiled sources
# (e.g. mscoree.cpp and exports.cpp for coreclr). add_pgo still sets the per-target LTO
# link flags needed by the final shared library.
# On Windows, PGO is handled entirely via link-time flags (/LTCG) so this isn't needed.
if(NOT WIN32)
    if(CLR_CMAKE_PGO_INSTRUMENT)
        if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
            if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
                add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:-fprofile-instr-generate>)
                # All shared libraries need to link the profiling runtime to resolve
                # instrumentation symbols from their static library dependencies.
                add_link_options(-fprofile-instr-generate)
            endif()
        endif()
    elseif(CLR_CMAKE_PGO_OPTIMIZE AND NOT CLR_CMAKE_ENABLE_SANITIZERS)
        file(TO_NATIVE_PATH "${CLR_CMAKE_OPTDATA_PATH}/data/coreclr.profdata" _PgoGlobalProfilePath)
        if(EXISTS "${_PgoGlobalProfilePath}")
            if(CMAKE_CXX_COMPILER_ID MATCHES "Clang")
                if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
                    check_have_lto_and_pgodata_supported("${_PgoGlobalProfilePath}")
                    if(HAVE_LTO_AND_PGO_DATA_SUPPORTED)
                        message(STATUS "Enabling profile guided optimizations globally for coreclr static libraries")
                        add_compile_options(
                            $<$<COMPILE_LANGUAGE:C,CXX>:-fprofile-instr-use=${_PgoGlobalProfilePath}>
                            $<$<COMPILE_LANGUAGE:C,CXX>:-Wno-profile-instr-out-of-date>
                            $<$<COMPILE_LANGUAGE:C,CXX>:-Wno-profile-instr-unprofiled>
                        )
                    endif()
                endif()
            endif()
        endif()
    endif()
endif()
