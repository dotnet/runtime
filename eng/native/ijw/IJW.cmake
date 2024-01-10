if (CLR_CMAKE_HOST_WIN32)

  function(remove_ijw_incompatible_options options updatedOptions)
    # IJW isn't compatible with Ehsc, which CMake enables by default
    set_property(DIRECTORY PROPERTY CLR_EH_OPTION "")

    # IJW isn't compatible with CFG
    set_property(DIRECTORY PROPERTY CLR_CONTROL_FLOW_GUARD OFF)

    # IJW isn't compatible with EHCONT, which requires CFG
    set_property(DIRECTORY PROPERTY CLR_EH_CONTINUATION OFF)

    # IJW isn't compatible with GR-
    if(options MATCHES "/GR-")
        string(REPLACE "/GR-" "" options "${options}")
    endif()

    # Disable native sanitizers for IJW since we don't want to have to locate
    # and copy the sanitizer runtimes and IJW must be built with a dynamic CRT.
    if (options MATCHES "-fsanitize=")
        string(REGEX REPLACE "-fsanitize=[a-zA-z,]+" "" options "${options}")
    endif()

    SET(${updatedOptions} "${options}" PARENT_SCOPE)
  endfunction()

  function(remove_ijw_incompatible_target_options targetName)
    get_target_property(compileOptions ${targetName} COMPILE_OPTIONS)
    remove_ijw_incompatible_options("${compileOptions}" compileOptions)
    set_target_properties(${targetName} PROPERTIES COMPILE_OPTIONS "${compileOptions}")
  endfunction()

  function(add_ijw_msbuild_project_properties targetName ijwhost_target)
    # When we're building with MSBuild, we need to set some project properties
    # in case CMake has decided to use the SDK support.
    # We're dogfooding things, so we need to set settings in ways that the product doesn't quite support.
    # We don't actually need an installed/available target framework version here
    # since we are disabling implicit framework references. We just need a valid value, and net8.0 is valid.
    set_target_properties(${targetName} PROPERTIES
      DOTNET_TARGET_FRAMEWORK net8.0
      VS_GLOBAL_DisableImplicitFrameworkReferences true
      VS_GLOBAL_GenerateRuntimeConfigurationFiles false
      VS_PROJECT_IMPORT "${CMAKE_CURRENT_FUNCTION_LIST_DIR}/SetIJWProperties.props")
  endfunction()

  # 4365 - signed/unsigned mismatch
  # 4679 - Could not import member. This is an issue with IJW and static abstract methods in interfaces.
  add_compile_options(/wd4365 /wd4679 /wd5271)

  # IJW
  add_compile_options(/clr:netcore)

  # IJW requires the CRT as a dll, not linked in
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:Debug>DLL)

  # CMake enables /RTC1 and /EHsc by default, but they're not compatible with /clr, so remove them
  if(CMAKE_CXX_FLAGS_DEBUG MATCHES "/RTC1")
    string(REPLACE "/RTC1" " " CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
  endif()

  remove_ijw_incompatible_options("${CMAKE_CXX_FLAGS}" CMAKE_CXX_FLAGS)

  get_directory_property(dirCompileOptions COMPILE_OPTIONS)
  remove_ijw_incompatible_options("${dirCompileOptions}" dirCompileOptions)
  set_directory_properties(PROPERTIES COMPILE_OPTIONS "${dirCompileOptions}")

  set(CLR_SDK_REF_PACK_OUTPUT "")
  set(CLR_SDK_REF_PACK_DISCOVERY_ERROR "")
  set(CLR_SDK_REF_PACK_DISCOVERY_RESULT 0)

  if (CPP_CLI_LIVE_REF_ASSEMBLIES)
    message("Using live-built ref assemblies for C++/CLI runtime tests.")
    execute_process(
        COMMAND powershell -ExecutionPolicy ByPass -NoProfile "${CMAKE_CURRENT_LIST_DIR}/getRefPackFolderFromArtifacts.ps1"
        OUTPUT_VARIABLE CLR_SDK_REF_PACK_OUTPUT
        OUTPUT_STRIP_TRAILING_WHITESPACE
        ERROR_VARIABLE CLR_SDK_REF_PACK_DISCOVERY_ERROR
        RESULT_VARIABLE CLR_SDK_REF_PACK_DISCOVERY_RESULT)
  else()
    execute_process(
        COMMAND powershell -ExecutionPolicy ByPass -NoProfile "${CMAKE_CURRENT_LIST_DIR}/getRefPackFolderFromSdk.ps1"
        OUTPUT_VARIABLE CLR_SDK_REF_PACK_OUTPUT
        OUTPUT_STRIP_TRAILING_WHITESPACE
        ERROR_VARIABLE CLR_SDK_REF_PACK_DISCOVERY_ERROR
        RESULT_VARIABLE CLR_SDK_REF_PACK_DISCOVERY_RESULT)
  endif()

  if (NOT CLR_SDK_REF_PACK_DISCOVERY_RESULT EQUAL 0)
    message(FATAL_ERROR "Unable to find reference assemblies: ${CLR_SDK_REF_PACK_DISCOVERY_ERROR}")
  endif()

  string(REGEX REPLACE ".*refPackPath=(.*)" "\\1" CLR_SDK_REF_PACK ${CLR_SDK_REF_PACK_OUTPUT})

  add_compile_options(/AI${CLR_SDK_REF_PACK})

  list(APPEND LINK_LIBRARIES_ADDITIONAL ijwhost)

endif()
