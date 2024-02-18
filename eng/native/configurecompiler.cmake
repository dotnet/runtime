# Due to how we build the libraries native build as part of the CoreCLR build as well as standalone,
# we can end up coming to this file twice. Only run it once to simplify our build.
include_guard()

include(${CMAKE_CURRENT_LIST_DIR}/configuretools.cmake)

# Set initial flags for each configuration

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(CheckCCompilerFlag)
include(CheckCXXCompilerFlag)

# "configureoptimization.cmake" must be included after CLR_CMAKE_HOST_UNIX has been set.
include(${CMAKE_CURRENT_LIST_DIR}/configureoptimization.cmake)

#-----------------------------------------------------
# Initialize Cmake compiler flags and other variables
#-----------------------------------------------------

if (CLR_CMAKE_HOST_UNIX)
    add_compile_options(-Wall)
    if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        add_compile_options(-Wno-null-conversion)
        add_compile_options(-glldb)
    else()
        add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Werror=conversion-null>)
        add_compile_options(-g)
    endif()
endif()

if (CMAKE_CONFIGURATION_TYPES) # multi-configuration generator?
    set(CMAKE_CONFIGURATION_TYPES "Debug;Checked;Release;RelWithDebInfo" CACHE STRING "" FORCE)
endif (CMAKE_CONFIGURATION_TYPES)

set(CMAKE_C_FLAGS_CHECKED "")
set(CMAKE_CXX_FLAGS_CHECKED "")
set(CMAKE_EXE_LINKER_FLAGS_CHECKED "")
set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "")

set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "")
set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "")
set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "")
set(CMAKE_EXE_LINKER_FLAGS_DEBUG "")
set(CMAKE_EXE_LINKER_FLAGS_DEBUG "")
set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "")

add_compile_definitions("$<$<CONFIG:DEBUG>:DEBUG;_DEBUG;_DBG;URTBLDENV_FRIENDLY=Debug;BUILDENV_DEBUG=1>")
add_compile_definitions("$<$<CONFIG:CHECKED>:DEBUG;_DEBUG;_DBG;URTBLDENV_FRIENDLY=Checked;BUILDENV_CHECKED=1>")
add_compile_definitions("$<$<OR:$<CONFIG:RELEASE>,$<CONFIG:RELWITHDEBINFO>>:NDEBUG;URTBLDENV_FRIENDLY=Retail>")

if (MSVC)

  define_property(TARGET PROPERTY CLR_CONTROL_FLOW_GUARD INHERITED BRIEF_DOCS "Controls the /guard:cf flag presence" FULL_DOCS "Set this property to ON or OFF to indicate if the /guard:cf compiler and linker flag should be present")
  define_property(TARGET PROPERTY CLR_EH_CONTINUATION INHERITED BRIEF_DOCS "Controls the /guard:ehcont flag presence" FULL_DOCS "Set this property to ON or OFF to indicate if the /guard:ehcont compiler flag should be present")
  define_property(TARGET PROPERTY CLR_EH_OPTION INHERITED BRIEF_DOCS "Defines the value of the /EH option" FULL_DOCS "Set this property to one of the valid /EHxx options (/EHa, /EHsc, /EHa-, ...)")

  set_property(GLOBAL PROPERTY CLR_CONTROL_FLOW_GUARD ON)

  # Remove the /EHsc from the CXX flags so that the compile options are the only source of truth for that
  string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  set_property(GLOBAL PROPERTY CLR_EH_OPTION /EHsc)

  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:$<TARGET_PROPERTY:CLR_EH_OPTION>>)
  add_link_options($<$<BOOL:$<TARGET_PROPERTY:CLR_CONTROL_FLOW_GUARD>>:/guard:cf>)

  # Linker flags
  #
  set (WINDOWS_SUBSYSTEM_VERSION 6.01)

  if (CLR_CMAKE_HOST_ARCH_ARM)
    set(WINDOWS_SUBSYSTEM_VERSION 6.02) #windows subsystem - arm minimum is 6.02
  elseif(CLR_CMAKE_HOST_ARCH_ARM64)
    set(WINDOWS_SUBSYSTEM_VERSION 6.03) #windows subsystem - arm64 minimum is 6.03
  endif ()

  #Do not create Side-by-Side Assembly Manifest
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /MANIFEST:NO")
  # can handle addresses larger than 2 gigabytes
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /LARGEADDRESSAWARE")
  #shrink pdb size
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /PDBCOMPRESS")

  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG")
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUGTYPE:CV,FIXUP")
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /IGNORE:4197,4013,4254,4070,4221")
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SUBSYSTEM:WINDOWS,${WINDOWS_SUBSYSTEM_VERSION}")

  set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /IGNORE:4221")

  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG")
  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUGTYPE:CV,FIXUP")
  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /PDBCOMPRESS")
  # For sanitized builds, we bump up the stack size to 8MB to match behavior on Unix platforms.
  # Sanitized builds can use significantly more stack space than non-sanitized builds due to instrumentation.
  # We don't want to change the default stack size for all builds, as that will likely cause confusion and will
  # increase memory usage.
  if (CLR_CMAKE_ENABLE_SANITIZERS)
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:0x800000")
  else()
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:0x180000")
  endif()

  if(EXISTS ${CLR_SOURCELINK_FILE_PATH})
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /sourcelink:${CLR_SOURCELINK_FILE_PATH}")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /sourcelink:${CLR_SOURCELINK_FILE_PATH}")
  endif(EXISTS ${CLR_SOURCELINK_FILE_PATH})

  if (CMAKE_GENERATOR MATCHES "^Visual Studio.*$")
    # Debug build specific flags
    # The Ninja generator doesn't appear to have the default `/INCREMENTAL:ON` that
    # the Visual Studio generator has. Therefore we will override the default for Visual Studio only.
    add_linker_flag(/INCREMENTAL:NO DEBUG)
    add_linker_flag(/OPT:NOREF DEBUG)
    add_linker_flag(/OPT:NOICF DEBUG)
  endif (CMAKE_GENERATOR MATCHES "^Visual Studio.*$")

  # Checked build specific flags
  add_linker_flag(/INCREMENTAL:NO CHECKED) # prevent "warning LNK4075: ignoring '/INCREMENTAL' due to '/OPT:REF' specification"
  add_linker_flag(/OPT:REF CHECKED)
  add_linker_flag(/OPT:NOICF CHECKED)

  # Release build specific flags
  add_linker_flag(/LTCG RELEASE)
  add_linker_flag(/OPT:REF RELEASE)
  add_linker_flag(/OPT:ICF RELEASE)
  add_linker_flag(/INCREMENTAL:NO RELEASE)
  set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /LTCG")

  # ReleaseWithDebugInfo build specific flags
  add_linker_flag(/LTCG RELWITHDEBINFO)
  add_linker_flag(/OPT:REF RELWITHDEBINFO)
  add_linker_flag(/OPT:ICF RELWITHDEBINFO)
  set(CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO} /LTCG")

elseif (CLR_CMAKE_HOST_UNIX)
  # Set the values to display when interactively configuring CMAKE_BUILD_TYPE
  set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "DEBUG;CHECKED;RELEASE;RELWITHDEBINFO")

  # Use uppercase CMAKE_BUILD_TYPE for the string comparisons below
  string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)

  if(CLR_CMAKE_HOST_BROWSER OR CLR_CMAKE_HOST_WASI)
    # The emscripten build has additional warnings so -Werror breaks
    add_compile_options(-Wno-unused-parameter)
    add_compile_options(-Wno-alloca)
    add_compile_options(-Wno-implicit-int-float-conversion)
  endif()
endif(MSVC)

if (CLR_CMAKE_ENABLE_SANITIZERS)
  set (CLR_CMAKE_BUILD_SANITIZERS "")
  set (CLR_CMAKE_SANITIZER_RUNTIMES "")
  string(FIND "${CLR_CMAKE_ENABLE_SANITIZERS}" "address" __ASAN_POS)
  if(${__ASAN_POS} GREATER -1)
    # Set up build flags for AddressSanitizer
    set (CLR_CMAKE_ENABLE_ASAN ON)
    if (MSVC)
      # /RTC1 is added by default by CMake and incompatible with ASAN, so remove it.
      string(REPLACE "/RTC1" "" CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG}")
      string(REPLACE "/RTC1" "" CMAKE_C_FLAGS_DEBUG "${CMAKE_C_FLAGS_DEBUG}")
      string(REPLACE "/RTC1" "" CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG}")
      string(REPLACE "/RTC1" "" CMAKE_EXE_LINKER_FLAGS_DEBUG "${CMAKE_EXE_LINKER_FLAGS_DEBUG}")
    endif()
    # For Mac and Windows platforms, we install the ASAN runtime next to the rest of our outputs to ensure that it's present when we execute our tests on Helix machines
    # The rest of our platforms use statically-linked ASAN so this isn't a concern for those platforms.
    if (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST)
      function(getSanitizerRuntimeDirectory output)
        enable_language(C)
        execute_process(
          COMMAND ${CMAKE_C_COMPILER} -print-resource-dir
          OUTPUT_VARIABLE compilerResourceDir
          OUTPUT_STRIP_TRAILING_WHITESPACE)
        set(${output} "${compilerResourceDir}/lib/darwin/" PARENT_SCOPE)
      endfunction()
      getSanitizerRuntimeDirectory(sanitizerRuntimeDirectory)
      find_library(ASAN_RUNTIME clang_rt.asan_osx_dynamic PATHS ${sanitizerRuntimeDirectory})
      add_compile_definitions(SANITIZER_SHARED_RUNTIME)
    elseif (CLR_CMAKE_TARGET_WIN32)
      function(getSanitizerRuntimeDirectory output archSuffixOutput)
        get_filename_component(compiler_directory "${CMAKE_C_COMPILER}" DIRECTORY)
        set(${output} "${compiler_directory}" PARENT_SCOPE)
        if (CLR_CMAKE_TARGET_ARCH_I386)
          set(${archSuffixOutput} "i386" PARENT_SCOPE)
        elseif (CLR_CMAKE_TARGET_ARCH_AMD64)
          set(${archSuffixOutput} "x86_64" PARENT_SCOPE)
        elseif (CLR_CMAKE_TARGET_ARCH_ARM)
          set(${archSuffixOutput} "armhf" PARENT_SCOPE)
        elseif (CLR_CMAKE_TARGET_ARCH_ARM64)
          set(${archSuffixOutput} "aarch64" PARENT_SCOPE)
        endif()
      endfunction()
      getSanitizerRuntimeDirectory(sanitizerRuntimeDirectory archSuffix)
      set(ASAN_RUNTIME "${sanitizerRuntimeDirectory}/clang_rt.asan_dynamic-${archSuffix}.dll")
      add_compile_definitions(SANITIZER_SHARED_RUNTIME)
    endif()
    if (CLR_CMAKE_ENABLE_ASAN)
      message("-- Address Sanitizer (asan) enabled")
      list(APPEND CLR_CMAKE_BUILD_SANITIZERS
        address)
      list(APPEND CLR_CMAKE_SANITIZER_RUNTIMES
        address)
      # We can't use preprocessor defines to determine if we're building with ASAN in assembly, so we'll
      # define the preprocessor define ourselves.
      add_compile_definitions($<$<COMPILE_LANGUAGE:ASM,ASM_MASM>:HAS_ADDRESS_SANITIZER>)

      # Disable the use-after-return check for ASAN on Clang. This is because we have a lot of code that
      # depends on the fact that our locals are not saved in a parallel stack, so we can't enable this today.
      # If we ever have a way to detect a parallel stack and track its bounds, we can re-enable this check.
      add_compile_options($<$<COMPILE_LANG_AND_ID:C,Clang>:-fsanitize-address-use-after-return=never>)
      add_compile_options($<$<COMPILE_LANG_AND_ID:CXX,Clang>:-fsanitize-address-use-after-return=never>)
    endif()
  endif()

  # Set up build flags for UBSanitizer
  if (CLR_CMAKE_HOST_UNIX)

    set (CLR_CMAKE_ENABLE_UBSAN OFF)
    # COMPAT: Allow enabling UBSAN in Debug/Checked builds via an environment variable.
    if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
      # obtain settings from running enablesanitizers.sh
      string(FIND "$ENV{DEBUG_SANITIZERS}" "ubsan" __UBSAN_ENV_POS)
      if (${__UBSAN_ENV_POS} GREATER -1)
        set(CLR_CMAKE_ENABLE_UBSAN ON)
      endif()
    endif()
    string(FIND "${CLR_CMAKE_ENABLE_SANITIZERS}" "undefined" __UBSAN_POS)
    if (${__UBSAN_POS} GREATER -1)
      set(CLR_CMAKE_ENABLE_UBSAN ON)
    endif()

    # set the CLANG sanitizer flags for debug build
    if(CLR_CMAKE_ENABLE_UBSAN)
      list(APPEND CLR_CMAKE_BUILD_SANITIZE_OPTIONS -fsanitize-ignorelist=${CMAKE_CURRENT_SOURCE_DIR}/sanitizer-ignorelist.txt)
      # all sanitizer flags are enabled except alignment (due to heavy use of __unaligned modifier)
      list(APPEND CLR_CMAKE_BUILD_SANITIZERS
        "bool"
        bounds
        enum
        float-cast-overflow
        float-divide-by-zero
        "function"
        integer
        nonnull-attribute
        null
        object-size
        "return"
        returns-nonnull-attribute
        shift
        unreachable
        vla-bound
        vptr)
      list(APPEND CLR_CMAKE_SANITIZER_RUNTIMES
        undefined)
      message("-- Undefined Behavior Sanitizer (ubsan) enabled")
    endif ()
  endif()
  list(JOIN CLR_CMAKE_BUILD_SANITIZERS "," CLR_CMAKE_BUILD_SANITIZERS)
  list(JOIN CLR_CMAKE_SANITIZER_RUNTIMES "," CLR_LINK_SANITIZERS_OPTIONS)
  if (CLR_CMAKE_BUILD_SANITIZERS)
    list(APPEND CLR_CMAKE_BUILD_SANITIZE_OPTIONS "-fsanitize=${CLR_CMAKE_BUILD_SANITIZERS}")
  endif()
  if (CLR_CMAKE_SANITIZER_RUNTIMES)
    list(APPEND CLR_CMAKE_LINK_SANITIZE_OPTIONS "-fsanitize=${CLR_CMAKE_SANITIZER_RUNTIMES}")
  endif()
  if (MSVC)
    add_compile_options("$<$<COMPILE_LANGUAGE:C,CXX>:${CLR_CMAKE_BUILD_SANITIZE_OPTIONS}>")
  else()
    add_compile_options("$<$<COMPILE_LANGUAGE:C,CXX>:${CLR_CMAKE_BUILD_SANITIZE_OPTIONS}>")
    add_linker_flag("${CLR_CMAKE_LINK_SANITIZE_OPTIONS}")
  endif()
endif()

# CLR_ADDITIONAL_LINKER_FLAGS - used for passing additional arguments to linker
# CLR_ADDITIONAL_COMPILER_OPTIONS - used for passing additional arguments to compiler
#
# For example:
#       ./build-native.sh cmakeargs "-DCLR_ADDITIONAL_COMPILER_OPTIONS=<...>" cmakeargs "-DCLR_ADDITIONAL_LINKER_FLAGS=<...>"
#
if(CLR_CMAKE_HOST_UNIX)
  foreach(ADDTL_LINKER_FLAG ${CLR_ADDITIONAL_LINKER_FLAGS})
    add_linker_flag(${ADDTL_LINKER_FLAG})
  endforeach()
endif(CLR_CMAKE_HOST_UNIX)

if(CLR_CMAKE_HOST_LINUX)
  add_compile_options($<$<COMPILE_LANGUAGE:ASM>:-Wa,--noexecstack>)
  add_linker_flag(-Wl,--build-id=sha1)
  add_linker_flag(-Wl,-z,relro,-z,now)
elseif(CLR_CMAKE_HOST_FREEBSD)
  add_compile_options($<$<COMPILE_LANGUAGE:ASM>:-Wa,--noexecstack>)
  add_linker_flag("-Wl,--build-id=sha1")
elseif(CLR_CMAKE_HOST_SUNOS)
  add_compile_options($<$<COMPILE_LANGUAGE:ASM>:-Wa,--noexecstack>)
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} -fstack-protector")
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} -fstack-protector")
  add_definitions(-D__EXTENSIONS__ -D_XPG4_2 -D_POSIX_PTHREAD_SEMANTICS)
elseif(CLR_CMAKE_HOST_OSX AND NOT CLR_CMAKE_HOST_MACCATALYST AND NOT CLR_CMAKE_HOST_IOS AND NOT CLR_CMAKE_HOST_TVOS)
  add_definitions(-D_XOPEN_SOURCE)
  add_linker_flag("-Wl,-bind_at_load")
elseif(CLR_CMAKE_HOST_HAIKU)
  add_compile_options($<$<COMPILE_LANGUAGE:ASM>:-Wa,--noexecstack>)
  add_linker_flag("-Wl,--no-undefined")
endif()

#------------------------------------
# Definitions (for platform)
#-----------------------------------
if (CLR_CMAKE_HOST_ARCH_AMD64)
  set(ARCH_HOST_NAME x64)
  add_definitions(-DHOST_AMD64 -DHOST_64BIT)
elseif (CLR_CMAKE_HOST_ARCH_I386)
  set(ARCH_HOST_NAME x86)
  add_definitions(-DHOST_X86)
elseif (CLR_CMAKE_HOST_ARCH_ARM)
  set(ARCH_HOST_NAME arm)
  add_definitions(-DHOST_ARM)
elseif (CLR_CMAKE_HOST_ARCH_ARMV6)
  set(ARCH_HOST_NAME armv6)
  add_definitions(-DHOST_ARM)
  add_definitions(-DHOST_ARMV6)
elseif (CLR_CMAKE_HOST_ARCH_ARM64)
  set(ARCH_HOST_NAME arm64)
  add_definitions(-DHOST_ARM64 -DHOST_64BIT)
elseif (CLR_CMAKE_HOST_ARCH_LOONGARCH64)
  set(ARCH_HOST_NAME loongarch64)
  add_definitions(-DHOST_LOONGARCH64 -DHOST_64BIT)
elseif (CLR_CMAKE_HOST_ARCH_RISCV64)
  set(ARCH_HOST_NAME riscv64)
  add_definitions(-DHOST_RISCV64 -DHOST_64BIT)
elseif (CLR_CMAKE_HOST_ARCH_S390X)
  set(ARCH_HOST_NAME s390x)
  add_definitions(-DHOST_S390X -DHOST_64BIT -DBIGENDIAN)
elseif (CLR_CMAKE_HOST_ARCH_WASM)
  set(ARCH_HOST_NAME wasm)
  add_definitions(-DHOST_WASM -DHOST_32BIT=1)
elseif (CLR_CMAKE_HOST_ARCH_MIPS64)
  set(ARCH_HOST_NAME mips64)
  add_definitions(-DHOST_MIPS64 -DHOST_64BIT=1)
elseif (CLR_CMAKE_HOST_ARCH_POWERPC64)
  set(ARCH_HOST_NAME ppc64le)
  add_definitions(-DHOST_POWERPC64 -DHOST_64BIT)
else ()
  clr_unknown_arch()
endif ()

if (CLR_CMAKE_HOST_UNIX)
  if(CLR_CMAKE_HOST_LINUX)
    if(CLR_CMAKE_HOST_UNIX_AMD64)
      message("Detected Linux x86_64")
    elseif(CLR_CMAKE_HOST_UNIX_ARM)
      message("Detected Linux arm")
    elseif(CLR_CMAKE_HOST_UNIX_ARMV6)
      message("Detected Linux armv6")
    elseif(CLR_CMAKE_HOST_UNIX_ARM64)
      message("Detected Linux arm64")
    elseif(CLR_CMAKE_HOST_UNIX_LOONGARCH64)
      message("Detected Linux loongarch64")
    elseif(CLR_CMAKE_HOST_UNIX_RISCV64)
      message("Detected Linux riscv64")
    elseif(CLR_CMAKE_HOST_UNIX_X86)
      message("Detected Linux i686")
    elseif(CLR_CMAKE_HOST_UNIX_S390X)
      message("Detected Linux s390x")
    elseif(CLR_CMAKE_HOST_UNIX_POWERPC64)
      message("Detected Linux ppc64le")
    else()
      clr_unknown_arch()
    endif()
  endif(CLR_CMAKE_HOST_LINUX)
endif(CLR_CMAKE_HOST_UNIX)

if (CLR_CMAKE_HOST_UNIX)
  add_definitions(-DHOST_UNIX)

  if(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)
    add_definitions(-DHOST_APPLE)
    if(CLR_CMAKE_HOST_MACCATALYST)
      add_definitions(-DHOST_MACCATALYST)
    else()
      add_definitions(-DHOST_OSX)
    endif()
    if(CLR_CMAKE_HOST_UNIX_AMD64)
      message("Detected OSX x86_64")
    elseif(CLR_CMAKE_HOST_UNIX_ARM64)
      message("Detected OSX ARM64")
    else()
      clr_unknown_arch()
    endif()
  elseif (CLR_CMAKE_HOST_IOS)
    add_definitions(-DHOST_APPLE)
    add_definitions(-DHOST_IOS)
    if(CLR_CMAKE_HOST_UNIX_AMD64)
      message("Detected iOS x86_64")
    elseif(CLR_CMAKE_HOST_UNIX_ARM64)
      message("Detected iOS ARM64")
    else()
      clr_unknown_arch()
    endif()
  elseif (CLR_CMAKE_HOST_TVOS)
    add_definitions(-DHOST_APPLE)
    add_definitions(-DHOST_TVOS)
    if(CLR_CMAKE_HOST_UNIX_AMD64)
      message("Detected tvOS x86_64")
    elseif(CLR_CMAKE_HOST_UNIX_ARM64)
      message("Detected tvOS ARM64")
    else()
      clr_unknown_arch()
    endif()
  elseif(CLR_CMAKE_HOST_FREEBSD)
    if(CLR_CMAKE_HOST_UNIX_ARM64)
      message("Detected FreeBSD aarch64")
    elseif(CLR_CMAKE_HOST_UNIX_AMD64)
      message("Detected FreeBSD amd64")
    else()
      message(FATAL_ERROR "Unsupported FreeBSD architecture")
    endif()
  elseif(CLR_CMAKE_HOST_NETBSD)
    message("Detected NetBSD amd64")
  elseif(CLR_CMAKE_HOST_SUNOS)
    message("Detected SunOS amd64")
  elseif(CLR_CMAKE_HOST_HAIKU)
    message("Detected Haiku x86_64")
  elseif(CLR_CMAKE_HOST_BROWSER)
    add_definitions(-DHOST_BROWSER)
  endif()
elseif(CLR_CMAKE_HOST_WASI)
  add_definitions(-DHOST_WASI)
endif()

if (CLR_CMAKE_HOST_WIN32)
  add_definitions(-DHOST_WINDOWS)

  # Define the CRT lib references that link into Desktop imports
  set(STATIC_MT_CRT_LIB  "libcmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
  set(STATIC_MT_VCRT_LIB  "libvcruntime$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
  set(STATIC_MT_CPP_LIB  "libcpmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
endif(CLR_CMAKE_HOST_WIN32)

# Unconditionally define _FILE_OFFSET_BITS as 64 on all platforms.
add_definitions(-D_FILE_OFFSET_BITS=64)

# Architecture specific files folder name
if (CLR_CMAKE_TARGET_ARCH_AMD64)
    set(ARCH_SOURCES_DIR amd64)
    set(ARCH_TARGET_NAME x64)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_AMD64>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_64BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_ARM64)
    set(ARCH_SOURCES_DIR arm64)
    set(ARCH_TARGET_NAME arm64)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_ARM64>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_64BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_ARM)
    set(ARCH_SOURCES_DIR arm)
    set(ARCH_TARGET_NAME arm)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_ARM>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_32BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_ARMV6)
    set(ARCH_SOURCES_DIR arm)
    set(ARCH_TARGET_NAME armv6)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_ARM>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_ARMV6>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_32BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_I386)
    set(ARCH_TARGET_NAME x86)
    set(ARCH_SOURCES_DIR i386)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_X86>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_32BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_LOONGARCH64)
    set(ARCH_TARGET_NAME loongarch64)
    set(ARCH_SOURCES_DIR loongarch64)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_LOONGARCH64>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_64BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_RISCV64)
    set(ARCH_TARGET_NAME riscv64)
    set(ARCH_SOURCES_DIR riscv64)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_RISCV64>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_64BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_S390X)
    set(ARCH_TARGET_NAME s390x)
    set(ARCH_SOURCES_DIR s390x)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_S390X>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_64BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_POWERPC64)
    set(ARCH_TARGET_NAME ppc64le)
    set(ARCH_SOURCES_DIR ppc64le)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_POWERPC64>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_64BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_WASM)
    set(ARCH_TARGET_NAME wasm)
    set(ARCH_SOURCES_DIR wasm)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_WASM>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_32BIT>)
elseif (CLR_CMAKE_TARGET_ARCH_MIPS64)
    set(ARCH_TARGET_NAME mips64)
    set(ARCH_SOURCES_DIR mips64)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_MIPS64>)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_64BIT>)
else ()
    clr_unknown_arch()
endif ()

#--------------------------------------
# Compile Options
#--------------------------------------
if (CLR_CMAKE_HOST_UNIX)
  # Disable frame pointer optimizations so profilers can get better call stacks
  add_compile_options(-fno-omit-frame-pointer)

  # The -fms-extensions enable the stuff like __if_exists, __declspec(uuid()), etc.
  add_compile_options(-fms-extensions)
  #-fms-compatibility      Enable full Microsoft Visual C++ compatibility
  #-fms-extensions         Accept some non-standard constructs supported by the Microsoft compiler

  # Make signed arithmetic overflow of addition, subtraction, and multiplication wrap around
  # using twos-complement representation (this is normally undefined according to the C++ spec).
  add_compile_options(-fwrapv)

  if(CLR_CMAKE_HOST_APPLE)
    # Clang will by default emit objc_msgSend stubs in Xcode 14, which ld from earlier Xcodes doesn't understand.
    # We disable this by passing -fno-objc-msgsend-selector-stubs to clang.
    # We can probably remove this flag once we require developers to use Xcode 14.
    # Ref: https://github.com/xamarin/xamarin-macios/issues/16223
    check_c_compiler_flag(-fno-objc-msgsend-selector-stubs COMPILER_SUPPORTS_FNO_OBJC_MSGSEND_SELECTOR_STUBS)
    if(COMPILER_SUPPORTS_FNO_OBJC_MSGSEND_SELECTOR_STUBS)
      set(CLR_CMAKE_COMMON_OBJC_FLAGS "${CLR_CMAKE_COMMON_OBJC_FLAGS} -fno-objc-msgsend-selector-stubs")
    endif()
  endif()

  if((CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST) AND CLR_CMAKE_HOST_UNIX_ARM64)
    # For osx-arm64, LSE instructions are enabled by default
    add_definitions(-DLSE_INSTRUCTIONS_ENABLED_BY_DEFAULT)
    add_compile_options(-mcpu=apple-m1)
  endif()

  # hardening options
  if(NOT CLR_CMAKE_HOST_BROWSER AND NOT CLR_CMAKE_HOST_WASI)
    check_c_compiler_flag(-fstack-protector-strong COMPILER_SUPPORTS_F_STACK_PROTECTOR_STRONG)
    if (COMPILER_SUPPORTS_F_STACK_PROTECTOR_STRONG)
      add_compile_options(-fstack-protector-strong)
    endif()

    check_c_compiler_flag(-fstack-clash-protection COMPILER_SUPPORTS_F_STACK_CLASH_PROTECTION)
    # explicit check for gcc is required because while clang does not implement stack-protection,
    # it does not return 'unknown argument' either, instead:
    #   clang: warning: argument unused during compilation: '-fstack-clash-protection' [-Wunused-command-line-argument]
    # see:
    #   https://github.com/llvm/llvm-project/issues/40148
    #   https://gitlab.kitware.com/cmake/cmake/-/issues/25390
    if (COMPILER_SUPPORTS_F_STACK_CLASH_PROTECTION AND CMAKE_C_COMPILER_ID STREQUAL "GNU")
      add_compile_options(-fstack-clash-protection)
    endif()

    check_c_compiler_flag(-fcf-protection COMPILER_SUPPORTS_F_CONTROL_FLOW_PROTECTION)
    if (COMPILER_SUPPORTS_F_CONTROL_FLOW_PROTECTION)
      add_compile_options(-fcf-protection)
    endif()

    # Enable maximum fortification
    remove_definitions(-D_FORTIFY_SOURCE)
    add_compile_definitions("$<$<CONFIG:RELEASE>:_FORTIFY_SOURCE=3>")
  endif()

  # Suppress warnings-as-errors in release branches to reduce servicing churn
  if (PRERELEASE)
    add_compile_options(-Werror)
  endif(PRERELEASE)

  # Disabled common warnings
  add_compile_options(-Wno-unused-variable)
  add_compile_options(-Wno-unused-value)
  add_compile_options(-Wno-unused-function)
  add_compile_options(-Wno-tautological-compare)
  add_compile_options(-Wno-unknown-pragmas)

  # Explicitly enabled warnings
  check_c_compiler_flag(-Wimplicit-fallthrough COMPILER_SUPPORTS_W_IMPLICIT_FALLTHROUGH)
  if (COMPILER_SUPPORTS_W_IMPLICIT_FALLTHROUGH)
    add_compile_options(-Wimplicit-fallthrough)
  endif()

  # VLAs are non standard in C++, aren't available on Windows and
  # are a warning by default since clang 18.
  # For consistency, enable warnings for all compiler versions.
  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wvla>)

  #These seem to indicate real issues
  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-invalid-offsetof>)

  add_compile_options(-Wno-unused-but-set-variable)

  # Turn off floating point expression contraction because it is considered a value changing
  # optimization in the IEEE 754 specification and is therefore considered unsafe.
  add_compile_options(-ffp-contract=off)

  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-fno-rtti>)

  if (CMAKE_C_COMPILER_ID MATCHES "Clang")
    add_compile_options(-Wno-unknown-warning-option)

    # The -ferror-limit is helpful during the porting, it makes sure the compiler doesn't stop
    # after hitting just about 20 errors.
    add_compile_options(-ferror-limit=4096)

    # Disabled warnings
    add_compile_options(-Wno-unused-private-field)
    # There are constants of type BOOL used in a condition. But BOOL is defined as int
    # and so the compiler thinks that there is a mistake.
    add_compile_options(-Wno-constant-logical-operand)
    # We use pshpack1/2/4/8.h and poppack.h headers to set and restore packing. However
    # clang 6.0 complains when the packing change lifetime is not contained within
    # a header file.
    add_compile_options(-Wno-pragma-pack)

    # The following warning indicates that an attribute __attribute__((__ms_struct__)) was applied
    # to a struct or a class that has virtual members or a base class. In that case, clang
    # may not generate the same object layout as MSVC.
    add_compile_options(-Wno-incompatible-ms-struct)

    add_compile_options(-Wno-reserved-identifier)

    # clang 16.0 introduced buffer hardening https://discourse.llvm.org/t/rfc-c-buffer-hardening/65734
    # which we are not conforming to yet.
    add_compile_options(-Wno-unsafe-buffer-usage)

    # other clang 16.0 suppressions
    add_compile_options(-Wno-single-bit-bitfield-constant-conversion)
    add_compile_options(-Wno-cast-function-type-strict)
  else()
    add_compile_options(-Wno-uninitialized)
    add_compile_options(-Wno-strict-aliasing)
    add_compile_options(-Wno-array-bounds)
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-misleading-indentation>)
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-stringop-overflow>)
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-restrict>)
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:-Wno-stringop-truncation>)
    add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-class-memaccess>)

    if (CMAKE_CXX_COMPILER_VERSION VERSION_LESS 12.0)
      # this warning is only reported by g++ 11 in debug mode when building
      # src/coreclr/vm/stackingallocator.h. It is a false-positive, fixed in g++ 12.
      # see: https://github.com/dotnet/runtime/pull/69188#issuecomment-1136764770
      add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-placement-new>)
    endif()

    if (CMAKE_CXX_COMPILER_ID)
      check_cxx_compiler_flag(-faligned-new COMPILER_SUPPORTS_F_ALIGNED_NEW)
      if (COMPILER_SUPPORTS_F_ALIGNED_NEW)
        add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-faligned-new>)
      endif()
    endif()
  endif()

  # Some architectures (e.g., ARM) assume char type is unsigned while CoreCLR assumes char is signed
  # as x64 does. It has been causing issues in ARM (https://github.com/dotnet/runtime/issues/5778)
  add_compile_options(-fsigned-char)

  # We mark the function which needs exporting with DLLEXPORT
  add_compile_options(-fvisibility=hidden)

  # Separate functions so linker can remove them.
  add_compile_options(-ffunction-sections)

  # Specify the minimum supported version of macOS
  # Mac Catalyst needs a special CFLAG, exclusive with mmacosx-version-min
  if(CLR_CMAKE_HOST_MACCATALYST)
    # Somewhere between CMake 3.17 and 3.19.4, it became impossible to not pass
    # a value for mmacosx-version-min (blank CMAKE_OSX_DEPLOYMENT_TARGET gets
    # replaced with a default value, and always gets expanded to an OS version.
    # https://gitlab.kitware.com/cmake/cmake/-/issues/20132
    # We need to disable the warning that -tagret replaces -mmacosx-version-min
    set(DISABLE_OVERRIDING_MIN_VERSION_ERROR -Wno-overriding-t-option)
    add_link_options(-Wno-overriding-t-option)
    if(CLR_CMAKE_HOST_ARCH_ARM64)
      set(MACOS_VERSION_MIN_FLAGS "-target arm64-apple-ios14.2-macabi")
      add_link_options(-target arm64-apple-ios14.2-macabi)
    elseif(CLR_CMAKE_HOST_ARCH_AMD64)
      set(MACOS_VERSION_MIN_FLAGS "-target x86_64-apple-ios13.5-macabi")
      add_link_options(-target x86_64-apple-ios13.5-macabi)
    else()
      clr_unknown_arch()
    endif()
    # These options are intentionally set using the CMAKE_XXX_FLAGS instead of
    # add_compile_options so that they take effect on the configuration functions
    # in various configure.cmake files.
    set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} ${MACOS_VERSION_MIN_FLAGS} ${DISABLE_OVERRIDING_MIN_VERSION_ERROR}")
    set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} ${MACOS_VERSION_MIN_FLAGS} ${DISABLE_OVERRIDING_MIN_VERSION_ERROR}")
    set(CMAKE_ASM_FLAGS "${CMAKE_ASM_FLAGS} ${MACOS_VERSION_MIN_FLAGS} ${DISABLE_OVERRIDING_MIN_VERSION_ERROR}")
    set(CMAKE_OBJC_FLAGS "${CMAKE_OBJC_FLAGS} ${MACOS_VERSION_MIN_FLAGS} ${DISABLE_OVERRIDING_MIN_VERSION_ERROR}")
    set(CMAKE_OBJCXX_FLAGS "${CMAKE_OBJCXX_FLAGS} ${MACOS_VERSION_MIN_FLAGS} ${DISABLE_OVERRIDING_MIN_VERSION_ERROR}")
  elseif(CLR_CMAKE_HOST_OSX)
    if(CLR_CMAKE_HOST_ARCH_ARM64)
      set(CMAKE_OSX_DEPLOYMENT_TARGET "11.0")
      add_compile_options(-arch arm64)
    elseif(CLR_CMAKE_HOST_ARCH_AMD64)
      set(CMAKE_OSX_DEPLOYMENT_TARGET "10.15")
      add_compile_options(-arch x86_64)
    else()
      clr_unknown_arch()
    endif()
  endif(CLR_CMAKE_HOST_MACCATALYST)

endif(CLR_CMAKE_HOST_UNIX)

if(CLR_CMAKE_TARGET_UNIX)
  add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_UNIX>)
  # Contracts are disabled on UNIX.
  add_definitions(-DDISABLE_CONTRACTS)
  if(CLR_CMAKE_TARGET_APPLE)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_APPLE>)
  endif()
  if(CLR_CMAKE_TARGET_OSX)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_OSX>)
  elseif(CLR_CMAKE_TARGET_MACCATALYST)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_MACCATALYST>)
  elseif(CLR_CMAKE_TARGET_IOS)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_IOS>)
  elseif(CLR_CMAKE_TARGET_TVOS)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_TVOS>)
  elseif(CLR_CMAKE_TARGET_FREEBSD)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_FREEBSD>)
  elseif(CLR_CMAKE_TARGET_ANDROID)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_ANDROID>)
  elseif(CLR_CMAKE_TARGET_LINUX)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_LINUX>)
    if(CLR_CMAKE_TARGET_LINUX_MUSL)
        add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_LINUX_MUSL>)
    endif()
  elseif(CLR_CMAKE_TARGET_NETBSD)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_NETBSD>)
  elseif(CLR_CMAKE_TARGET_SUNOS)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_SUNOS>)
    if(CLR_CMAKE_TARGET_OS_ILLUMOS)
      add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_ILLUMOS>)
    endif()
  elseif(CLR_CMAKE_TARGET_HAIKU)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_HAIKU>)
  endif()
  if(CLR_CMAKE_TARGET_BROWSER)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_BROWSER>)
  endif()
elseif(CLR_CMAKE_TARGET_WASI)
  add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_WASI>)
else(CLR_CMAKE_TARGET_UNIX)
  add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_WINDOWS>)
endif(CLR_CMAKE_TARGET_UNIX)

if(CLR_CMAKE_HOST_UNIX_ARM)
   if (NOT DEFINED CLR_ARM_FPU_TYPE)
     set(CLR_ARM_FPU_TYPE vfpv3)
   endif(NOT DEFINED CLR_ARM_FPU_TYPE)

   # Because we don't use CMAKE_C_COMPILER/CMAKE_CXX_COMPILER to use clang
   # we have to set the triple by adding a compiler argument
   add_compile_options(-mthumb)
   add_compile_options(-mfpu=${CLR_ARM_FPU_TYPE})
   if (NOT DEFINED CLR_ARM_FPU_CAPABILITY)
     set(CLR_ARM_FPU_CAPABILITY 0x7)
   endif(NOT DEFINED CLR_ARM_FPU_CAPABILITY)
   add_definitions(-DCLR_ARM_FPU_CAPABILITY=${CLR_ARM_FPU_CAPABILITY})
   add_compile_options(-march=armv7-a)
   if(ARM_SOFTFP)
     add_definitions(-DARM_SOFTFP)
     add_compile_options(-mfloat-abi=softfp)
   endif(ARM_SOFTFP)
endif(CLR_CMAKE_HOST_UNIX_ARM)

if(CLR_CMAKE_HOST_UNIX_ARMV6)
   add_compile_options(-mfpu=vfp)
   add_definitions(-DCLR_ARM_FPU_CAPABILITY=0x0)
   add_compile_options(-march=armv6zk)
   add_compile_options(-mcpu=arm1176jzf-s)
   add_compile_options(-mfloat-abi=hard)
endif(CLR_CMAKE_HOST_UNIX_ARMV6)

if(CLR_CMAKE_HOST_UNIX_X86)
  add_compile_options(-msse2)
endif()

if(CLR_CMAKE_HOST_UNIX)
  add_compile_options(${CLR_ADDITIONAL_COMPILER_OPTIONS})
endif(CLR_CMAKE_HOST_UNIX)

if (MSVC)
  # Compile options for targeting windows

  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/nologo>) # Suppress Startup Banner

  # [[! Microsoft.Security.SystemsADM.10086 !]] - SDL required warnings
  # set default warning level to 4 but allow targets to override it.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/W$<GENEX_EVAL:$<IF:$<BOOL:$<TARGET_PROPERTY:MSVC_WARNING_LEVEL>>,$<TARGET_PROPERTY:MSVC_WARNING_LEVEL>,4>>>)
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/WX>) # treat warnings as errors
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Oi>) # enable intrinsics
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Oy->) # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Gm->) # disable minimal rebuild
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zp8>) # pack structs on 8-byte boundary
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Gy>) # separate functions for linker
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/GS>) # Explicitly enable the buffer security checks
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/fp:precise>) # Enable precise floating point

  # Disable C++ RTTI
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /GR-")

  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/FC>) # use full pathnames in diagnostics
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/MP>) # Build with Multiple Processes (number of processes equal to the number of processors)
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zm200>) # Specify Precompiled Header Memory Allocation Limit of 150MB
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zc:strictStrings>) # Disable string-literal to char* or wchar_t* conversion
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zc:wchar_t>) # wchar_t is a built-in type.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zc:inline>) # All inline functions must have their definition available in the current translation unit.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zc:forScope>) # Enforce standards-compliant for scope.

  # Disable Warnings:
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4065>) # switch statement contains 'default' but no 'case' labels
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4100>) # 'identifier' : unreferenced formal parameter
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4127>) # conditional expression is constant
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4131>) # 'function' : uses old-style declarator
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4189>) # local variable is initialized but not referenced
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4200>) # nonstandard extension used : zero-sized array in struct/union
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4201>) # nonstandard extension used : nameless struct/union
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4206>) # nonstandard extension used : translation unit is empty
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4239>) # nonstandard extension used : 'token' : conversion from 'type' to 'type'
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4245>) # conversion from 'type1' to 'type2', signed/unsigned mismatch
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4291>) # no matching operator delete found; memory will not be freed if initialization throws an exception
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4310>) # cast truncates constant value
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4324>) # 'struct_name' : structure was padded due to __declspec(align())
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4366>) # The result of the unary 'operator' operator may be unaligned
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4456>) # declaration of 'identifier' hides previous local declaration
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4457>) # declaration of 'identifier' hides function parameter
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4458>) # declaration of 'identifier' hides class member
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4459>) # declaration of 'identifier' hides global declaration
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4463>) # overflow; assigning value to bit-field that can only hold values from low_value to high_value
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4505>) # unreferenced function with internal linkage has been removed
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4702>) # unreachable code
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4706>) # assignment within conditional expression
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4733>) # Inline asm assigning to 'FS:0' : handler not registered as safe handler
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4815>) # 'var': zero-sized array in stack object will have no elements (unless the object is an aggregate that has been aggregate initialized)
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4838>) # conversion from 'type_1' to 'type_2' requires a narrowing conversion
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4918>) # 'character' : invalid character in pragma optimization list
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4960>) # 'function' is too big to be profiled
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4961>) # No profile data was merged into '.pgd file', profile-guided optimizations disabled
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd5105>) # macro expansion producing 'defined' has undefined behavior
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd5205>) # delete of an abstract class 'type-name' that has a non-virtual destructor results in undefined behavior

  # Treat Warnings as Errors:
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4007>) # 'main' : must be __cdecl.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4013>) # 'function' undefined - assuming extern returning int.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4102>) # "'%$S' : unreferenced label".
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4551>) # Function call missing argument list.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4700>) # Local used w/o being initialized.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4640>) # 'instance' : construction of local static object is not thread-safe
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/we4806>) # Unsafe operation involving type 'bool'.

  # Set Warning Level 3:
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w34092>) # Sizeof returns 'unsigned long'.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w34121>) # Structure is sensitive to alignment.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w34125>) # Decimal digit in octal sequence.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w34130>) # Logical operation on address of string constant.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w34132>) # Const object should be initialized.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w34212>) # Function declaration used ellipsis.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w34530>) # C++ exception handler used, but unwind semantics are not enabled. Specify -GX.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w35038>) # data member 'member1' will be initialized after data member 'member2'.

  # Set Warning Level 4:
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/w44177>) # Pragma data_seg s/b at global scope.

  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX,ASM_MASM>:/Zi>) # enable debugging information
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/ZH:SHA_256>) # use SHA256 for generating hashes of compiler processed source files.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/source-charset:utf-8>) # Force MSVC to compile source as UTF-8.

  if (CLR_CMAKE_HOST_ARCH_I386)
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Gz>)
  endif (CLR_CMAKE_HOST_ARCH_I386)

  set(CMAKE_INTERPROCEDURAL_OPTIMIZATION ON)
  set(CMAKE_INTERPROCEDURAL_OPTIMIZATION_DEBUG OFF)
  set(CMAKE_INTERPROCEDURAL_OPTIMIZATION_CHECKED OFF)

  if (CLR_CMAKE_HOST_ARCH_AMD64)
    # The generator expression in the following command means that the /homeparams option is added only for debug builds for C and C++ source files
    add_compile_options($<$<AND:$<CONFIG:Debug>,$<COMPILE_LANGUAGE:C,CXX>>:/homeparams>) # Force parameters passed in registers to be written to the stack
  endif (CLR_CMAKE_HOST_ARCH_AMD64)

  # enable control-flow-guard support for native components for non-Arm64 builds
  # Added using variables instead of add_compile_options to let individual projects override it
  add_compile_options($<$<AND:$<COMPILE_LANGUAGE:C,CXX>,$<BOOL:$<TARGET_PROPERTY:CLR_CONTROL_FLOW_GUARD>>>:/guard:cf>)

  # Enable EH-continuation table and CET-compatibility for native components for amd64 builds except for components of the Mono
  # runtime. Added some switches using variables instead of add_compile_options to let individual projects override it.
  if (CLR_CMAKE_HOST_ARCH_AMD64 AND NOT CLR_CMAKE_RUNTIME_MONO)
    set_property(GLOBAL PROPERTY CLR_EH_CONTINUATION ON)

    add_compile_options($<$<AND:$<COMPILE_LANGUAGE:C,CXX,ASM_MASM>,$<BOOL:$<TARGET_PROPERTY:CLR_EH_CONTINUATION>>>:/guard:ehcont>)
    add_link_options($<$<BOOL:$<TARGET_PROPERTY:CLR_EH_CONTINUATION>>:/guard:ehcont>)
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /CETCOMPAT")
  endif (CLR_CMAKE_HOST_ARCH_AMD64 AND NOT CLR_CMAKE_RUNTIME_MONO)

  # Statically linked CRT (libcmt[d].lib, libvcruntime[d].lib and libucrt[d].lib) by default. This is done to avoid
  # linking in VCRUNTIME140.DLL for a simplified xcopy experience by reducing the dependency on VC REDIST.
  #
  # For Release builds, we shall dynamically link into uCRT [ucrtbase.dll] (which is pushed down as a Windows Update on downlevel OS) but
  # wont do the same for debug/checked builds since ucrtbased.dll is not redistributable and Debug/Checked builds are not
  # production-time scenarios.
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded$<$<AND:$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>,$<NOT:$<BOOL:$<TARGET_PROPERTY:DAC_COMPONENT>>>>:Debug>)

  if (NOT CLR_CMAKE_ENABLE_SANITIZERS)
    # Force uCRT to be dynamically linked for Release build
    # We won't do this for sanitized builds as the dynamic CRT is not compatible with the static sanitizer runtime and
    # the dynamic sanitizer runtime is not redistributable. Sanitized runtime builds are not production-time scenarios
    # so we don't get the benefits of a dynamic CRT for sanitized runtime builds.
    add_linker_flag(/NODEFAULTLIB:libucrt.lib RELEASE)
    add_linker_flag(/DEFAULTLIB:ucrt.lib RELEASE)
  endif()

  add_compile_options($<$<COMPILE_LANGUAGE:ASM_MASM>:/ZH:SHA_256>)

  if (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)
    # Contracts work too slow on ARM/ARM64 DEBUG/CHECKED.
    add_definitions(-DDISABLE_CONTRACTS)
  endif (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)

  # Don't display the output header when building RC files.
  set(CMAKE_RC_FLAGS "${CMAKE_RC_FLAGS} /nologo")
  # Don't display the output header when building asm files.
  set(CMAKE_ASM_MASM_FLAGS "${CMAKE_ASM_MASM_FLAGS} /nologo")
endif (MSVC)

# Configure non-MSVC compiler flags that apply to all platforms (unix-like or otherwise)
if (NOT MSVC)
  # Check for sometimes suppressed warnings
  check_c_compiler_flag(-Wreserved-identifier COMPILER_SUPPORTS_W_RESERVED_IDENTIFIER)
  if(COMPILER_SUPPORTS_W_RESERVED_IDENTIFIER)
    add_compile_definitions(COMPILER_SUPPORTS_W_RESERVED_IDENTIFIER)
  endif()
endif()

if(CLR_CMAKE_ENABLE_CODE_COVERAGE)

  if(CLR_CMAKE_HOST_UNIX)
    string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
    if(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
      message( WARNING "Code coverage results with an optimised (non-Debug) build may be misleading" )
    endif(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)

    add_compile_options(-fprofile-arcs)
    add_compile_options(-ftest-coverage)
    add_linker_flag(--coverage)
  else()
    message(FATAL_ERROR "Code coverage builds not supported on current platform")
  endif(CLR_CMAKE_HOST_UNIX)

endif(CLR_CMAKE_ENABLE_CODE_COVERAGE)

if (CMAKE_GENERATOR MATCHES "(Makefile|Ninja)")
  set(CMAKE_RC_CREATE_SHARED_LIBRARY "${CMAKE_CXX_CREATE_SHARED_LIBRARY}")
endif()

# Ensure other tools are present
if (CLR_CMAKE_HOST_WIN32)
    if(CLR_CMAKE_HOST_ARCH_ARM)

      # Explicitly specify the assembler to be used for Arm32 compile
      file(TO_CMAKE_PATH "$ENV{VCToolsInstallDir}\\bin\\HostX86\\arm\\armasm.exe" CMAKE_ASM_COMPILER)

      set(CMAKE_ASM_MASM_COMPILER ${CMAKE_ASM_COMPILER})
      message("CMAKE_ASM_MASM_COMPILER explicitly set to: ${CMAKE_ASM_MASM_COMPILER}")

      # Enable generic assembly compilation to avoid CMake generate VS proj files that explicitly
      # use ml[64].exe as the assembler.
      enable_language(ASM)
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreaded         "")
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDLL      "")
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebug    "")
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebugDLL "")
      set(CMAKE_ASM_COMPILE_OBJECT "<CMAKE_ASM_COMPILER> -g <INCLUDES> <FLAGS> -o <OBJECT> <SOURCE>")

    elseif(CLR_CMAKE_HOST_ARCH_ARM64)

      # Explicitly specify the assembler to be used for Arm64 compile
      file(TO_CMAKE_PATH "$ENV{VCToolsInstallDir}\\bin\\HostX86\\arm64\\armasm64.exe" CMAKE_ASM_COMPILER)

      set(CMAKE_ASM_MASM_COMPILER ${CMAKE_ASM_COMPILER})
      message("CMAKE_ASM_MASM_COMPILER explicitly set to: ${CMAKE_ASM_MASM_COMPILER}")

      # Enable generic assembly compilation to avoid CMake generate VS proj files that explicitly
      # use ml[64].exe as the assembler.
      enable_language(ASM)
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreaded         "")
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDLL      "")
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebug    "")
      set(CMAKE_ASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebugDLL "")
      set(CMAKE_ASM_COMPILE_OBJECT "<CMAKE_ASM_COMPILER> -g <INCLUDES> <FLAGS> -o <OBJECT> <SOURCE>")
    else()
      enable_language(ASM_MASM)
      set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreaded         "")
      set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDLL      "")
      set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebug    "")
      set(CMAKE_ASM_MASM_COMPILE_OPTIONS_MSVC_RUNTIME_LIBRARY_MultiThreadedDebugDLL "")
    endif()

    # Ensure that MC is present
    find_program(MC mc)
    if (MC STREQUAL "MC-NOTFOUND")
        message(FATAL_ERROR "MC not found")
    endif()

elseif (NOT CLR_CMAKE_HOST_BROWSER AND NOT CLR_CMAKE_HOST_WASI)
    # This is a workaround for upstream issue: https://gitlab.kitware.com/cmake/cmake/-/issues/22995.
    #
    # In Clang.cmake, the decision to use single or double hyphen for target and gcc-toolchain
    # is made based on CMAKE_${LANG}_COMPILER_VERSION, but CMAKE_ASM_COMPILER_VERSION is empty
    # so it picks up single hyphen options, which new clang versions don't recognize.
    set (CMAKE_ASM_COMPILER_VERSION "${CMAKE_C_COMPILER_VERSION}")

    enable_language(ASM)

endif(CLR_CMAKE_HOST_WIN32)
