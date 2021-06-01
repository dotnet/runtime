include(${CMAKE_CURRENT_LIST_DIR}/configuretools.cmake)

# Set initial flags for each configuration

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

include(CheckCXXCompilerFlag)

# "configureoptimization.cmake" must be included after CLR_CMAKE_HOST_UNIX has been set.
include(${CMAKE_CURRENT_LIST_DIR}/configureoptimization.cmake)

#-----------------------------------------------------
# Initialize Cmake compiler flags and other variables
#-----------------------------------------------------

if (CLR_CMAKE_HOST_UNIX)
    add_compile_options(-g)
    add_compile_options(-Wall)
    if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
        add_compile_options(-Wno-null-conversion)
    else()
        add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Werror=conversion-null>)
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

add_compile_definitions("$<$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>:DEBUG;_DEBUG;_DBG;URTBLDENV_FRIENDLY=Checked;BUILDENV_CHECKED=1>")
add_compile_definitions("$<$<OR:$<CONFIG:RELEASE>,$<CONFIG:RELWITHDEBINFO>>:NDEBUG;URTBLDENV_FRIENDLY=Retail>")

if (MSVC)
  add_linker_flag(/GUARD:CF)

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
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /IGNORE:4197,4013,4254,4070,4221")
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SUBSYSTEM:WINDOWS,${WINDOWS_SUBSYSTEM_VERSION}")

  set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /IGNORE:4221")

  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG")
  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /PDBCOMPRESS")
  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:1572864")

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

  # Force uCRT to be dynamically linked for Release build
  add_linker_flag(/NODEFAULTLIB:libucrt.lib RELEASE)
  add_linker_flag(/DEFAULTLIB:ucrt.lib RELEASE)

elseif (CLR_CMAKE_HOST_UNIX)
  # Set the values to display when interactively configuring CMAKE_BUILD_TYPE
  set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "DEBUG;CHECKED;RELEASE;RELWITHDEBINFO")

  # Use uppercase CMAKE_BUILD_TYPE for the string comparisons below
  string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)

  set(CLR_SANITIZE_CXX_OPTIONS "")
  set(CLR_SANITIZE_LINK_OPTIONS "")

  # set the CLANG sanitizer flags for debug build
  if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
    # obtain settings from running enablesanitizers.sh
    string(FIND "$ENV{DEBUG_SANITIZERS}" "asan" __ASAN_POS)
    string(FIND "$ENV{DEBUG_SANITIZERS}" "ubsan" __UBSAN_POS)
    if ((${__ASAN_POS} GREATER -1) OR (${__UBSAN_POS} GREATER -1))
      list(APPEND CLR_SANITIZE_CXX_OPTIONS -fsanitize-blacklist=${CMAKE_CURRENT_SOURCE_DIR}/sanitizerblacklist.txt)
      set (CLR_CXX_SANITIZERS "")
      set (CLR_LINK_SANITIZERS "")
      if (${__ASAN_POS} GREATER -1)
        list(APPEND CLR_CXX_SANITIZERS address)
        list(APPEND CLR_LINK_SANITIZERS address)
        set(CLR_SANITIZE_CXX_FLAGS "${CLR_SANITIZE_CXX_FLAGS}address,")
        set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}address,")
        add_definitions(-DHAS_ASAN)
        message("Address Sanitizer (asan) enabled")
      endif ()
      if (${__UBSAN_POS} GREATER -1)
        # all sanitizier flags are enabled except alignment (due to heavy use of __unaligned modifier)
        list(APPEND CLR_CXX_SANITIZERS
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
        list(APPEND CLR_LINK_SANITIZERS
          undefined)
        message("Undefined Behavior Sanitizer (ubsan) enabled")
      endif ()
      list(JOIN CLR_CXX_SANITIZERS "," CLR_CXX_SANITIZERS_OPTIONS)
      list(APPEND CLR_SANITIZE_CXX_OPTIONS "-fsanitize=${CLR_CXX_SANITIZERS_OPTIONS}")
      list(JOIN CLR_LINK_SANITIZERS "," CLR_LINK_SANITIZERS_OPTIONS)
      list(APPEND CLR_SANITIZE_LINK_OPTIONS "-fsanitize=${CLR_LINK_SANITIZERS_OPTIONS}")

      # -fdata-sections -ffunction-sections: each function has own section instead of one per .o file (needed for --gc-sections)
      # -O1: optimization level used instead of -O0 to avoid compile error "invalid operand for inline asm constraint"
      add_compile_options("$<$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>:${CLR_SANITIZE_CXX_OPTIONS};-fdata-sections;--ffunction-sections;-O1>")
      add_linker_flag("${CLR_SANITIZE_LINK_OPTIONS}" DEBUG CHECKED)
      # -Wl and --gc-sections: drop unused sections\functions (similar to Windows /Gy function-level-linking)
      add_linker_flag("-Wl,--gc-sections" DEBUG CHECKED)
    endif ()
  endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
endif(MSVC)

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
  add_definitions(-D__EXTENSIONS__)
elseif(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)
  add_definitions(-D_XOPEN_SOURCE)
endif()

#------------------------------------
# Definitions (for platform)
#-----------------------------------
if (CLR_CMAKE_HOST_ARCH_AMD64)
  set(ARCH_HOST_NAME x64)
  add_definitions(-DHOST_AMD64)
  add_definitions(-DHOST_64BIT)
elseif (CLR_CMAKE_HOST_ARCH_I386)
  set(ARCH_HOST_NAME x86)
  add_definitions(-DHOST_X86)
elseif (CLR_CMAKE_HOST_ARCH_ARM)
  set(ARCH_HOST_NAME arm)
  add_definitions(-DHOST_ARM)
elseif (CLR_CMAKE_HOST_ARCH_ARM64)
  set(ARCH_HOST_NAME arm64)
  add_definitions(-DHOST_ARM64)
  add_definitions(-DHOST_64BIT)
else ()
  clr_unknown_arch()
endif ()

if (CLR_CMAKE_HOST_UNIX)
  if(CLR_CMAKE_HOST_LINUX)
    if(CLR_CMAKE_HOST_UNIX_AMD64)
      message("Detected Linux x86_64")
    elseif(CLR_CMAKE_HOST_UNIX_ARM)
      message("Detected Linux ARM")
    elseif(CLR_CMAKE_HOST_UNIX_ARM64)
      message("Detected Linux ARM64")
    elseif(CLR_CMAKE_HOST_UNIX_X86)
      message("Detected Linux i686")
    else()
      clr_unknown_arch()
    endif()
  endif(CLR_CMAKE_HOST_LINUX)
endif(CLR_CMAKE_HOST_UNIX)

if (CLR_CMAKE_HOST_UNIX)
  add_definitions(-DHOST_UNIX)

  if(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)
    add_definitions(-DHOST_OSX)
    if(CLR_CMAKE_HOST_UNIX_AMD64)
      message("Detected OSX x86_64")
    elseif(CLR_CMAKE_HOST_UNIX_ARM64)
      message("Detected OSX ARM64")
    else()
      clr_unknown_arch()
    endif()
  elseif(CLR_CMAKE_HOST_FREEBSD)
    message("Detected FreeBSD amd64")
  elseif(CLR_CMAKE_HOST_NETBSD)
    message("Detected NetBSD amd64")
  elseif(CLR_CMAKE_HOST_SUNOS)
    message("Detected SunOS amd64")
  endif(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)
endif(CLR_CMAKE_HOST_UNIX)

if (CLR_CMAKE_HOST_WIN32)
  add_definitions(-DHOST_WINDOWS)

  # Define the CRT lib references that link into Desktop imports
  set(STATIC_MT_CRT_LIB  "libcmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
  set(STATIC_MT_VCRT_LIB  "libvcruntime$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
  set(STATIC_MT_CPP_LIB  "libcpmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
endif(CLR_CMAKE_HOST_WIN32)

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
elseif (CLR_CMAKE_TARGET_ARCH_I386)
    set(ARCH_TARGET_NAME x86)
    set(ARCH_SOURCES_DIR i386)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_ARCH>>>:TARGET_X86>)
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

  if(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)
    # We cannot enable "stack-protector-strong" on OS X due to a bug in clang compiler (current version 7.0.2)
    add_compile_options(-fstack-protector)
  else()
    check_cxx_compiler_flag(-fstack-protector-strong COMPILER_SUPPORTS_F_STACK_PROTECTOR_STRONG)
    if (COMPILER_SUPPORTS_F_STACK_PROTECTOR_STRONG)
      add_compile_options(-fstack-protector-strong)
    endif()
  endif(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)

  # Suppress warnings-as-errors in release branches to reduce servicing churn
  if (PRERELEASE)
    add_compile_options(-Werror)
  endif(PRERELEASE)

  # Disabled common warnings
  add_compile_options(-Wno-unused-variable)
  add_compile_options(-Wno-unused-value)
  add_compile_options(-Wno-unused-function)
  add_compile_options(-Wno-tautological-compare)

  check_cxx_compiler_flag(-Wimplicit-fallthrough COMPILER_SUPPORTS_W_IMPLICIT_FALLTHROUGH)
  if (COMPILER_SUPPORTS_W_IMPLICIT_FALLTHROUGH)
    add_compile_options(-Wimplicit-fallthrough)
  endif()

  #These seem to indicate real issues
  add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-invalid-offsetof>)

  if (CMAKE_CXX_COMPILER_ID MATCHES "Clang")
    # The -ferror-limit is helpful during the porting, it makes sure the compiler doesn't stop
    # after hitting just about 20 errors.
    add_compile_options(-ferror-limit=4096)

    # Disabled warnings
    add_compile_options(-Wno-unused-private-field)
    # Explicit constructor calls are not supported by clang (this->ClassName::ClassName())
    add_compile_options(-Wno-microsoft)
    # There are constants of type BOOL used in a condition. But BOOL is defined as int
    # and so the compiler thinks that there is a mistake.
    add_compile_options(-Wno-constant-logical-operand)
    # We use pshpack1/2/4/8.h and poppack.h headers to set and restore packing. However
    # clang 6.0 complains when the packing change lifetime is not contained within
    # a header file.
    add_compile_options(-Wno-pragma-pack)

    add_compile_options(-Wno-unknown-warning-option)

    # The following warning indicates that an attribute __attribute__((__ms_struct__)) was applied
    # to a struct or a class that has virtual members or a base class. In that case, clang
    # may not generate the same object layout as MSVC.
    add_compile_options(-Wno-incompatible-ms-struct)
  else()
    add_compile_options(-Wno-unused-but-set-variable)
    add_compile_options(-Wno-unknown-pragmas)
    add_compile_options(-Wno-uninitialized)
    add_compile_options(-Wno-strict-aliasing)
    add_compile_options(-Wno-array-bounds)
    check_cxx_compiler_flag(-Wclass-memaccess COMPILER_SUPPORTS_W_CLASS_MEMACCESS)
    if (COMPILER_SUPPORTS_W_CLASS_MEMACCESS)
      add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-Wno-class-memaccess>)
    endif()
    check_cxx_compiler_flag(-faligned-new COMPILER_SUPPORTS_F_ALIGNED_NEW)
    if (COMPILER_SUPPORTS_F_ALIGNED_NEW)
      add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-faligned-new>)
    endif()
  endif()

  # Some architectures (e.g., ARM) assume char type is unsigned while CoreCLR assumes char is signed
  # as x64 does. It has been causing issues in ARM (https://github.com/dotnet/runtime/issues/5778)
  add_compile_options(-fsigned-char)

  # We mark the function which needs exporting with DLLEXPORT
  add_compile_options(-fvisibility=hidden)

  # Specify the minimum supported version of macOS
  if(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)
    # Mac Catalyst needs a special CFLAG, exclusive with mmacosx-version-min
    if(CLR_CMAKE_TARGET_MACCATALYST)
      # Somewhere between CMake 3.17 and 3.19.4, it became impossible to not pass
      # a value for mmacosx-version-min (blank CMAKE_OSX_DEPLOYMENT_TARGET gets
      # replaced with a default value, and always gets expanded to an OS version.
      # https://gitlab.kitware.com/cmake/cmake/-/issues/20132
      # We need to disable the warning that -tagret replaces -mmacosx-version-min
      add_compile_options(-Wno-overriding-t-option)
      add_link_options(-Wno-overriding-t-option)
      if(CLR_CMAKE_HOST_ARCH_ARM64)
        add_compile_options(-target arm64-apple-ios14.2-macabi)
        add_link_options(-target arm64-apple-ios14.2-macabi)
      elseif(CLR_CMAKE_HOST_ARCH_AMD64)
        add_compile_options(-target x86_64-apple-ios13.5-macabi)
        add_link_options(-target x86_64-apple-ios13.5-macabi)
      else()
        clr_unknown_arch()
      endif()
    else()
      if(CLR_CMAKE_HOST_ARCH_ARM64)
        # 'pthread_jit_write_protect_np' is only available on macOS 11.0 or newer
        set(MACOS_VERSION_MIN_FLAGS -mmacosx-version-min=11.0)
        add_compile_options(-arch arm64)
      elseif(CLR_CMAKE_HOST_ARCH_AMD64)
        set(MACOS_VERSION_MIN_FLAGS -mmacosx-version-min=10.13)
        add_compile_options(-arch x86_64)
      else()
        clr_unknown_arch()
      endif()
      add_compile_options(${MACOS_VERSION_MIN_FLAGS})
      add_linker_flag(${MACOS_VERSION_MIN_FLAGS})
    endif(CLR_CMAKE_TARGET_MACCATALYST)
  endif(CLR_CMAKE_HOST_OSX OR CLR_CMAKE_HOST_MACCATALYST)

endif(CLR_CMAKE_HOST_UNIX)

if(CLR_CMAKE_TARGET_UNIX)
  add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_UNIX>)
  # Contracts are disabled on UNIX.
  add_definitions(-DDISABLE_CONTRACTS)
  if(CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_OSX>)
  elseif(CLR_CMAKE_TARGET_FREEBSD)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_FREEBSD>)
  elseif(CLR_CMAKE_TARGET_LINUX)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_LINUX>)
  elseif(CLR_CMAKE_TARGET_NETBSD)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_NETBSD>)
  elseif(CLR_CMAKE_TARGET_SUNOS)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_SUNOS>)
  elseif(CLR_CMAKE_TARGET_ANDROID)
    add_compile_definitions($<$<NOT:$<BOOL:$<TARGET_PROPERTY:IGNORE_DEFAULT_TARGET_OS>>>:TARGET_ANDROID>)
  endif()
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

if(CLR_CMAKE_HOST_UNIX_X86)
  add_compile_options(-msse2)
endif()

if(CLR_CMAKE_HOST_UNIX)
  add_compile_options(${CLR_ADDITIONAL_COMPILER_OPTIONS})
endif(CLR_CMAKE_HOST_UNIX)

if (MSVC)
  # Compile options for targeting windows

  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/nologo>) # Suppress Startup Banner
  # /W3 is added by default by CMake, so remove it
  string(REPLACE "/W3" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
  string(REPLACE "/W3" "" CMAKE_C_FLAGS "${CMAKE_C_FLAGS}")
  # set default warning level to 3 but allow targets to override it.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/W$<GENEX_EVAL:$<IF:$<BOOL:$<TARGET_PROPERTY:MSVC_WARNING_LEVEL>>,$<TARGET_PROPERTY:MSVC_WARNING_LEVEL>,3>>>)
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/WX>) # treat warnings as errors
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Oi>) # enable intrinsics
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Oy->) # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Gm->) # disable minimal rebuild
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zp8>) # pack structs on 8-byte boundary
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Gy>) # separate functions for linker
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/GS>) # Explicitly enable the buffer security checks
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/fp:precise>) # Enable precise floating point

  # disable C++ RTTI
  # /GR is added by default by CMake, so remove it manually.
  string(REPLACE "/GR" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
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
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4189>) # local variable is initialized but not referenced
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4200>) # nonstandard extension used : zero-sized array in struct/union
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4201>) # nonstandard extension used : nameless struct/union
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4245>) # conversion from 'type1' to 'type2', signed/unsigned mismatch
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4291>) # no matching operator delete found; memory will not be freed if initialization throws an exception
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4456>) # declaration of 'identifier' hides previous local declaration
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4457>) # declaration of 'identifier' hides function parameter
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4458>) # declaration of 'identifier' hides class member
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4733>) # Inline asm assigning to 'FS:0' : handler not registered as safe handler
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4838>) # conversion from 'type_1' to 'type_2' requires a narrowing conversion
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4960>) # 'function' is too big to be profiled
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd4961>) # No profile data was merged into '.pgd file', profile-guided optimizations disabled
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/wd5105>) # macro expansion producing 'defined' has undefined behavior

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

  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Zi>) # enable debugging information
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/ZH:SHA_256>) # use SHA256 for generating hashes of compiler processed source files.
  add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/source-charset:utf-8>) # Force MSVC to compile source as UTF-8.

  if (CLR_CMAKE_HOST_ARCH_I386)
    add_compile_options($<$<COMPILE_LANGUAGE:C,CXX>:/Gz>)
  endif (CLR_CMAKE_HOST_ARCH_I386)

  add_compile_options($<$<AND:$<COMPILE_LANGUAGE:C,CXX>,$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>>:/GL>)

  if (CLR_CMAKE_HOST_ARCH_AMD64)
    # The generator expression in the following command means that the /homeparams option is added only for debug builds for C and C++ source files
    add_compile_options($<$<AND:$<CONFIG:Debug>,$<COMPILE_LANGUAGE:C,CXX>>:/homeparams>) # Force parameters passed in registers to be written to the stack
  endif (CLR_CMAKE_HOST_ARCH_AMD64)

  # enable control-flow-guard support for native components for non-Arm64 builds
  # Added using variables instead of add_compile_options to let individual projects override it
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /guard:cf")
  set(CMAKE_C_FLAGS "${CMAKE_C_FLAGS} /guard:cf")

  # Statically linked CRT (libcmt[d].lib, libvcruntime[d].lib and libucrt[d].lib) by default. This is done to avoid
  # linking in VCRUNTIME140.DLL for a simplified xcopy experience by reducing the dependency on VC REDIST.
  #
  # For Release builds, we shall dynamically link into uCRT [ucrtbase.dll] (which is pushed down as a Windows Update on downlevel OS) but
  # wont do the same for debug/checked builds since ucrtbased.dll is not redistributable and Debug/Checked builds are not
  # production-time scenarios.
  set(CMAKE_MSVC_RUNTIME_LIBRARY MultiThreaded$<$<AND:$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>,$<NOT:$<BOOL:$<TARGET_PROPERTY:DAC_COMPONENT>>>>:Debug>)

  add_compile_options($<$<COMPILE_LANGUAGE:ASM_MASM>:/ZH:SHA_256>)

  if (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)
    # Contracts work too slow on ARM/ARM64 DEBUG/CHECKED.
    add_definitions(-DDISABLE_CONTRACTS)
  endif (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)

  # Don't display the output header when building RC files.
  add_compile_options($<$<COMPILE_LANGUAGE:RC>:/nologo>)
endif (MSVC)

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

else (CLR_CMAKE_HOST_WIN32)
    enable_language(ASM)

endif(CLR_CMAKE_HOST_WIN32)
