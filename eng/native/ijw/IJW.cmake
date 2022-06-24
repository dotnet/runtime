if (CLR_CMAKE_HOST_WIN32)

  function(remove_ijw_incompatible_options options updatedOptions)

    # IJW isn't compatible with Ehsc, which CMake enables by default,
    if(options MATCHES "/EHsc")
        string(REPLACE "/EHsc" "" options "${options}")
    endif()

    # IJW isn't compatible with CFG
    if(options MATCHES "/guard:cf")
        string(REPLACE "/guard:cf" "" options "${options}")
    endif()

    # IJW isn't compatible with EHCONT, which requires CFG
    if(options MATCHES "/guard:ehcont")
        string(REPLACE "/guard:ehcont" "" options "${options}")
    endif()

    # IJW isn't compatible with GR-
    if(options MATCHES "/GR-")
        string(REPLACE "/GR-" "" options "${options}")
    endif()

    SET(${updatedOptions} "${options}" PARENT_SCOPE)
  endfunction()

  function(remove_ijw_incompatible_target_options targetName)
    get_target_property(compileOptions ${targetName} COMPILE_OPTIONS)
    remove_ijw_incompatible_options("${compileOptions}" compileOptions)
    set_target_properties(${targetName} PROPERTIES COMPILE_OPTIONS "${compileOptions}")
  endfunction()

  # 4365 - signed/unsigned mismatch
  # 4679 - Could not import member. This is an issue with IJW and static abstract methods in interfaces. 
  add_compile_options(/wd4365 /wd4679)

  # IJW
  add_compile_options(/clr:netcore)

  # IJW requires the CRT as a dll, not linked in
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:Debug>DLL)

  # CMake enables /RTC1 and /EHsc by default, but they're not compatible with /clr, so remove them
  if(CMAKE_CXX_FLAGS_DEBUG MATCHES "/RTC1")
    string(REPLACE "/RTC1" " " CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
  endif()

  remove_ijw_incompatible_options("${CMAKE_CXX_FLAGS}" CMAKE_CXX_FLAGS)

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
