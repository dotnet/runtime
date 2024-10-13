include(${CMAKE_CURRENT_LIST_DIR}/configureplatform.cmake)

# Get the version of the compiler that is in the file name for tool location.
set (CLR_CMAKE_COMPILER_FILE_NAME_VERSION "")
if (CMAKE_C_COMPILER MATCHES "-?[0-9]+(\.[0-9]+)?$")
  set(CLR_CMAKE_COMPILER_FILE_NAME_VERSION "${CMAKE_MATCH_0}")
endif()

if(NOT WIN32 AND NOT CLR_CMAKE_TARGET_BROWSER AND NOT CLR_CMAKE_TARGET_WASI)
  if(CMAKE_C_COMPILER_ID MATCHES "Clang")
    if(APPLE)
      set(TOOLSET_PREFIX "")
    else()
      set(TOOLSET_PREFIX "llvm-")
    endif()
  elseif(CMAKE_C_COMPILER_ID MATCHES "GNU")
    if(CMAKE_CROSSCOMPILING)
      set(TOOLSET_PREFIX "${CMAKE_C_COMPILER_TARGET}-")
    else()
      set(TOOLSET_PREFIX "")
    endif()
  endif()

  function(locate_toolchain_exec exec var required)
    string(TOUPPER ${exec} EXEC_UPPERCASE)
    if(NOT "$ENV{CLR_${EXEC_UPPERCASE}}" STREQUAL "")
      set(${var} "$ENV{CLR_${EXEC_UPPERCASE}}" PARENT_SCOPE)
      return()
    endif()

    unset(EXEC_LOCATION_${exec} CACHE)
    find_program(EXEC_LOCATION_${exec}
      NAMES
      "${TOOLSET_PREFIX}${exec}${CLR_CMAKE_COMPILER_FILE_NAME_VERSION}"
      "${TOOLSET_PREFIX}${exec}")

    if (required AND EXEC_LOCATION_${exec} STREQUAL "EXEC_LOCATION_${exec}-NOTFOUND")
      message(FATAL_ERROR "Unable to find toolchain executable. Name: '${exec}', Prefix: '${TOOLSET_PREFIX}'")
    endif()

    if (NOT EXEC_LOCATION_${exec} STREQUAL "EXEC_LOCATION_${exec}-NOTFOUND")
      set(${var} ${EXEC_LOCATION_${exec}} PARENT_SCOPE)
    endif()
  endfunction()

  locate_toolchain_exec(ar CMAKE_AR YES)
  locate_toolchain_exec(nm CMAKE_NM YES)
  locate_toolchain_exec(ranlib CMAKE_RANLIB YES)
  locate_toolchain_exec(strings CMAKE_STRINGS YES)

  if(CMAKE_C_COMPILER_ID MATCHES "Clang")
    locate_toolchain_exec(link CMAKE_LINKER YES)
  endif()

  if(NOT CLR_CMAKE_TARGET_APPLE AND (NOT CLR_CMAKE_TARGET_ANDROID OR CROSS_ROOTFS))
    locate_toolchain_exec(objdump CMAKE_OBJDUMP YES)
    locate_toolchain_exec(readelf CMAKE_READELF YES)

    unset(CMAKE_OBJCOPY CACHE)
    locate_toolchain_exec(objcopy CMAKE_OBJCOPY NO)

    if (CMAKE_OBJCOPY)
      execute_process(
        COMMAND ${CMAKE_OBJCOPY} --help
        OUTPUT_VARIABLE OBJCOPY_HELP_OUTPUT
      )
    endif()

    # if llvm-objcopy does not support --only-keep-debug argument, try to locate binutils' objcopy
    if (NOT CMAKE_OBJCOPY OR (CMAKE_C_COMPILER_ID MATCHES "Clang" AND NOT "${OBJCOPY_HELP_OUTPUT}" MATCHES "--only-keep-debug"))
      set(TOOLSET_PREFIX "")
      locate_toolchain_exec(objcopy CMAKE_OBJCOPY YES)
    endif ()

  endif()
endif()

if (NOT CLR_CMAKE_HOST_WIN32)
  # detect linker
  execute_process(COMMAND sh -c "${CMAKE_C_COMPILER} ${CMAKE_SHARED_LINKER_FLAGS} -Wl,--version | head -1"
    ERROR_QUIET
    OUTPUT_VARIABLE ldVersionOutput
    OUTPUT_STRIP_TRAILING_WHITESPACE)

  if("${ldVersionOutput}" MATCHES "LLD")
    set(LD_LLVM 1)
  elseif("${ldVersionOutput}" MATCHES "GNU ld" OR "${ldVersionOutput}" MATCHES "GNU gold" OR "${ldVersionOutput}" MATCHES "GNU linkers")
    set(LD_GNU 1)
  elseif("${ldVersionOutput}" MATCHES "Solaris Link")
    set(LD_SOLARIS 1)
  else(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)
    set(LD_OSX 1)
  endif()
  message("-- The linker identification is ${ldVersionOutput}")
endif()

# This introspection depends on CMAKE_STRINGS, which is why it's in this file instead of configureplatform
if (CLR_CMAKE_HOST_LINUX)
  execute_process(
    COMMAND bash -c "if ${CMAKE_STRINGS} \"${CMAKE_SYSROOT}/usr/bin/ldd\" 2>&1 | grep -q musl; then echo musl; fi"
    OUTPUT_VARIABLE CLR_CMAKE_LINUX_MUSL
    OUTPUT_STRIP_TRAILING_WHITESPACE)

  if(CLR_CMAKE_LINUX_MUSL STREQUAL musl)
    set(CLR_CMAKE_HOST_LINUX_MUSL 1)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_LINUX 1)
    set(CLR_CMAKE_TARGET_LINUX_MUSL 1)
  endif()
endif()
