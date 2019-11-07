# Set initial flags for each configuration

set(CMAKE_EXPORT_COMPILE_COMMANDS ON)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_CXX_STANDARD 11)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

cmake_policy(SET CMP0083 NEW)

include(CheckPIESupported)
include(CheckCXXCompilerFlag)

# All code we build should be compiled as position independent
check_pie_supported(OUTPUT_VARIABLE PIE_SUPPORT_OUTPUT LANGUAGES CXX)
if(NOT MSVC AND NOT CMAKE_CXX_LINK_PIE_SUPPORTED)
  message(WARNING "PIE is not supported at link time: ${PIE_SUPPORT_OUTPUT}.\n"
                  "PIE link options will not be passed to linker.")
endif()
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

#----------------------------------------
# Detect and set platform variable names
#     - for non-windows build platform & architecture is detected using inbuilt CMAKE variables and cross target component configure
#     - for windows we use the passed in parameter to CMAKE to determine build arch
#----------------------------------------
if(CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    if(CLR_CROSS_COMPONENTS_BUILD)
        # CMAKE_HOST_SYSTEM_PROCESSOR returns the value of `uname -p` on host.
        if(CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL amd64)
            if(CLR_CMAKE_TARGET_ARCH STREQUAL "arm" OR CLR_CMAKE_TARGET_ARCH STREQUAL "armel")
                if(CMAKE_CROSSCOMPILING)
                    set(CLR_CMAKE_PLATFORM_UNIX_X86 1)
                else()
                    set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
                endif()
            else()
                set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
            endif()
        elseif(CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL i686)
            set(CLR_CMAKE_PLATFORM_UNIX_X86 1)
        else()
            clr_unknown_arch()
        endif()
    else()
        # CMAKE_SYSTEM_PROCESSOR returns the value of `uname -p` on target.
        # For the AMD/Intel 64bit architecture two different strings are common.
        # Linux and Darwin identify it as "x86_64" while FreeBSD and netbsd uses the
        # "amd64" string. Accept either of the two here.
        if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64)
            set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l)
            set(CLR_CMAKE_PLATFORM_UNIX_ARM 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
            set(CLR_CMAKE_PLATFORM_UNIX_ARM 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
            set(CLR_CMAKE_PLATFORM_UNIX_ARM64 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
            set(CLR_CMAKE_PLATFORM_UNIX_X86 1)
        else()
            clr_unknown_arch()
        endif()
    endif()
    set(CLR_CMAKE_PLATFORM_LINUX 1)

    # Detect Linux ID
    set(LINUX_ID_FILE "/etc/os-release")
    if(CMAKE_CROSSCOMPILING)
        set(LINUX_ID_FILE "${CMAKE_SYSROOT}${LINUX_ID_FILE}")
    endif()

    execute_process(
        COMMAND bash -c "source ${LINUX_ID_FILE} && echo \$ID"
        OUTPUT_VARIABLE CLR_CMAKE_LINUX_ID
        OUTPUT_STRIP_TRAILING_WHITESPACE)

    if(DEFINED CLR_CMAKE_LINUX_ID)
        if(CLR_CMAKE_LINUX_ID STREQUAL tizen)
            set(CLR_CMAKE_TARGET_TIZEN_LINUX 1)
        elseif(CLR_CMAKE_LINUX_ID STREQUAL alpine)
            set(CLR_CMAKE_PLATFORM_ALPINE_LINUX 1)
        endif()
    endif(DEFINED CLR_CMAKE_LINUX_ID)
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
  set(CLR_CMAKE_PLATFORM_DARWIN 1)
  set(CMAKE_ASM_COMPILE_OBJECT "${CMAKE_C_COMPILER} <FLAGS> <DEFINES> <INCLUDES> -o <OBJECT> -c <SOURCE>")
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
  set(CLR_CMAKE_PLATFORM_FREEBSD 1)
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
  set(CLR_CMAKE_PLATFORM_OPENBSD 1)
endif(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)

if(CMAKE_SYSTEM_NAME STREQUAL NetBSD)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
  set(CLR_CMAKE_PLATFORM_NETBSD 1)
endif(CMAKE_SYSTEM_NAME STREQUAL NetBSD)

if(CMAKE_SYSTEM_NAME STREQUAL SunOS)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  EXECUTE_PROCESS(
    COMMAND isainfo -n
    OUTPUT_VARIABLE SUNOS_NATIVE_INSTRUCTION_SET
    )
  if(SUNOS_NATIVE_INSTRUCTION_SET MATCHES "amd64")
    set(CLR_CMAKE_PLATFORM_UNIX_AMD64 1)
    set(CMAKE_SYSTEM_PROCESSOR "amd64")
  else()
    clr_unknown_arch()
  endif()
  set(CLR_CMAKE_PLATFORM_SUNOS 1)
endif(CMAKE_SYSTEM_NAME STREQUAL SunOS)

# "configureoptimization.cmake" must be included after CLR_CMAKE_PLATFORM_UNIX has been set.
include(${CMAKE_CURRENT_LIST_DIR}/configureoptimization.cmake)

#--------------------------------------------
# This repo builds two set of binaries
# 1. binaries which execute on target arch machine
#        - for such binaries host architecture & target architecture are same
#        - eg. coreclr.dll
# 2. binaries which execute on host machine but target another architecture
#        - host architecture is different from target architecture
#        - eg. crossgen.exe - runs on x64 machine and generates nis targeting arm64
#        - for complete list of such binaries refer to file crosscomponents.cmake
#-------------------------------------------------------------
# Set HOST architecture variables
if(CLR_CMAKE_PLATFORM_UNIX_ARM)
    set(CLR_CMAKE_PLATFORM_ARCH_ARM 1)
    set(CLR_CMAKE_HOST_ARCH "arm")
elseif(CLR_CMAKE_PLATFORM_UNIX_ARM64)
    set(CLR_CMAKE_PLATFORM_ARCH_ARM64 1)
    set(CLR_CMAKE_HOST_ARCH "arm64")
elseif(CLR_CMAKE_PLATFORM_UNIX_AMD64)
    set(CLR_CMAKE_PLATFORM_ARCH_AMD64 1)
    set(CLR_CMAKE_HOST_ARCH "x64")
elseif(CLR_CMAKE_PLATFORM_UNIX_X86)
    set(CLR_CMAKE_PLATFORM_ARCH_I386 1)
    set(CLR_CMAKE_HOST_ARCH "x86")
elseif(WIN32)
    # CLR_CMAKE_HOST_ARCH is passed in as param to cmake
    if (CLR_CMAKE_HOST_ARCH STREQUAL x64)
        set(CLR_CMAKE_PLATFORM_ARCH_AMD64 1)
    elseif(CLR_CMAKE_HOST_ARCH STREQUAL x86)
        set(CLR_CMAKE_PLATFORM_ARCH_I386 1)
    elseif(CLR_CMAKE_HOST_ARCH STREQUAL arm)
        set(CLR_CMAKE_PLATFORM_ARCH_ARM 1)
    elseif(CLR_CMAKE_HOST_ARCH STREQUAL arm64)
        set(CLR_CMAKE_PLATFORM_ARCH_ARM64 1)
    else()
        clr_unknown_arch()
    endif()
endif()

# Set TARGET architecture variables
# Target arch will be a cmake param (optional) for both windows as well as non-windows build
# if target arch is not specified then host & target are same
if(NOT DEFINED CLR_CMAKE_TARGET_ARCH OR CLR_CMAKE_TARGET_ARCH STREQUAL "" )
  set(CLR_CMAKE_TARGET_ARCH ${CLR_CMAKE_HOST_ARCH})
endif()

# Set target architecture variables
if (CLR_CMAKE_TARGET_ARCH STREQUAL x64)
    set(CLR_CMAKE_TARGET_ARCH_AMD64 1)
  elseif(CLR_CMAKE_TARGET_ARCH STREQUAL x86)
    set(CLR_CMAKE_TARGET_ARCH_I386 1)
  elseif(CLR_CMAKE_TARGET_ARCH STREQUAL arm64)
    set(CLR_CMAKE_TARGET_ARCH_ARM64 1)
  elseif(CLR_CMAKE_TARGET_ARCH STREQUAL arm)
    set(CLR_CMAKE_TARGET_ARCH_ARM 1)
  elseif(CLR_CMAKE_TARGET_ARCH STREQUAL armel)
    set(CLR_CMAKE_TARGET_ARCH_ARM 1)
    set(ARM_SOFTFP 1)
  else()
    clr_unknown_arch()
endif()

# check if host & target arch combination are valid
if(NOT(CLR_CMAKE_TARGET_ARCH STREQUAL CLR_CMAKE_HOST_ARCH))
    if(NOT((CLR_CMAKE_PLATFORM_ARCH_AMD64 AND CLR_CMAKE_TARGET_ARCH_ARM64) OR (CLR_CMAKE_PLATFORM_ARCH_I386 AND CLR_CMAKE_TARGET_ARCH_ARM) OR (CLR_CMAKE_PLATFORM_ARCH_AMD64 AND CLR_CMAKE_TARGET_ARCH_ARM)))
        message(FATAL_ERROR "Invalid host and target arch combination")
    endif()
endif()

#-----------------------------------------------------
# Initialize Cmake compiler flags and other variables
#-----------------------------------------------------

if(MSVC)
    add_compile_options(/Zi /FC /Zc:strictStrings)
elseif (CLR_CMAKE_PLATFORM_UNIX)
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

add_compile_definitions("$<$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>:DEBUG;_DEBUG;_DBG;URTBLDENV_FRIENDLY=Checked;BUILDENV_CHECKED=1>")
add_compile_definitions("$<$<OR:$<CONFIG:RELEASE>,$<CONFIG:RELWITHDEBINFO>>:NDEBUG;URTBLDENV_FRIENDLY=Retail>")

set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.

if (MSVC)
  add_link_options(/GUARD:CF)

  # Linker flags
  #
  set (WINDOWS_SUBSYSTEM_VERSION 6.01)

  if (CLR_CMAKE_PLATFORM_ARCH_ARM)
    set(WINDOWS_SUBSYSTEM_VERSION 6.02) #windows subsystem - arm minimum is 6.02
  elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    set(WINDOWS_SUBSYSTEM_VERSION 6.03) #windows subsystem - arm64 minimum is 6.03
  endif ()

  #Do not create Side-by-Side Assembly Manifest
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/MANIFEST:NO>)
  # can handle addresses larger than 2 gigabytes
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/LARGEADDRESSAWARE>)
  #Compatible with Data Execution Prevention
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/NXCOMPAT>)
  #Use address space layout randomization
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/DYNAMICBASE>)
  #shrink pdb size
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/PDBCOMPRESS>)

  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/DEBUG>)
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/IGNORE:4197,4013,4254,4070,4221>)
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>:/SUBSYSTEM:WINDOWS,${WINDOWS_SUBSYSTEM_VERSION}>)

  set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /IGNORE:4221")

  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,EXECUTABLE>:/DEBUG>)
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,EXECUTABLE>:/PDBCOMPRESS>)
  add_link_options($<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,EXECUTABLE>:/STACK:1572864>)

  # Debug build specific flags
  add_link_options($<$<AND:$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>,$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>>:/NOVCFEATURE>)

  # Checked build specific flags
  add_link_options($<$<CONFIG:CHECKED>:/OPT:REF>)
  add_link_options($<$<CONFIG:CHECKED>:/OPT:NOICF>)

  # Release build specific flags
  add_link_options($<$<CONFIG:RELEASE>:/LTCG>)
  add_link_options($<$<CONFIG:RELEASE>:/OPT:REF>)
  add_link_options($<$<CONFIG:RELEASE>:/OPT:ICF>)
  set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /LTCG")

  # ReleaseWithDebugInfo build specific flags
  add_link_options($<$<CONFIG:RELWITHDEBINFO>:/LTCG>)
  add_link_options($<$<CONFIG:RELWITHDEBINFO>:/OPT:REF>)
  add_link_options($<$<CONFIG:RELWITHDEBINFO>:/OPT:ICF>)
  set(CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO} /LTCG")

  # Force uCRT to be dynamically linked for Release build
  add_link_options("$<$<CONFIG:RELEASE>:/NODEFAULTLIB:libucrt.lib;/DEFAULTLIB:ucrt.lib>")

elseif (CLR_CMAKE_PLATFORM_UNIX)
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
      add_compile_definitions("$<$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>:${CLR_SANITIZE_CXX_OPTIONS};-fdata-sections;--ffunction-sections;-O1>")
      add_link_options($<$<AND:$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>,$<STREQUAL:$<TARGET_PROPERTY:TYPE>,EXECUTABLE>>:${CLR_SANITIZE_LINK_OPTIONS}>)

      # -Wl and --gc-sections: drop unused sections\functions (similar to Windows /Gy function-level-linking)
      add_link_options("$<$<AND:$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>,$<STREQUAL:$<TARGET_PROPERTY:TYPE>,SHARED_LIBRARY>>:${CLR_SANITIZE_LINK_OPTIONS};-Wl,--gc-sections>")
    endif ()
  endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
endif(MSVC)

# CLR_ADDITIONAL_LINKER_FLAGS - used for passing additional arguments to linker
# CLR_ADDITIONAL_COMPILER_OPTIONS - used for passing additional arguments to compiler
#
# For example:
#       ./build-native.sh cmakeargs "-DCLR_ADDITIONAL_COMPILER_OPTIONS=<...>" cmakeargs "-DCLR_ADDITIONAL_LINKER_FLAGS=<...>"
#
if(CLR_CMAKE_PLATFORM_UNIX)
    add_link_options(${CLR_ADDITIONAL_LINKER_FLAGS})
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CLR_CMAKE_PLATFORM_LINUX)
  add_compile_options($<$<COMPILE_LANGUAGE:ASM>:-Wa,--noexecstack>)
  add_link_options(-Wl,--build-id=sha1)
endif(CLR_CMAKE_PLATFORM_LINUX)
if(CLR_CMAKE_PLATFORM_FREEBSD)
  add_compile_options($<$<COMPILE_LANGUAGE:ASM>:-Wa,--noexecstack>)
  add_link_options(-fuse-ld=lld LINKER:--build-id=sha1)
endif(CLR_CMAKE_PLATFORM_FREEBSD)

#------------------------------------
# Definitions (for platform)
#-----------------------------------
if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-D_AMD64_)
  add_definitions(-DAMD64)
  add_definitions(-DBIT64)
elseif (CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-D_X86_)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-D_ARM_)
  add_definitions(-DARM)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-D_ARM64_)
  add_definitions(-DARM64)
  add_definitions(-DBIT64)
else ()
  clr_unknown_arch()
endif ()

if (CLR_CMAKE_PLATFORM_UNIX)
  if(CLR_CMAKE_PLATFORM_LINUX)
    if(CLR_CMAKE_PLATFORM_UNIX_AMD64)
      message("Detected Linux x86_64")
      add_definitions(-DLINUX64)
    elseif(CLR_CMAKE_PLATFORM_UNIX_ARM)
      message("Detected Linux ARM")
      add_definitions(-DLINUX32)
    elseif(CLR_CMAKE_PLATFORM_UNIX_ARM64)
      message("Detected Linux ARM64")
      add_definitions(-DLINUX64)
    elseif(CLR_CMAKE_PLATFORM_UNIX_X86)
      message("Detected Linux i686")
      add_definitions(-DLINUX32)
    else()
      clr_unknown_arch()
    endif()
  endif(CLR_CMAKE_PLATFORM_LINUX)
endif(CLR_CMAKE_PLATFORM_UNIX)

if (CLR_CMAKE_PLATFORM_UNIX)
  add_definitions(-DPLATFORM_UNIX)

  if(CLR_CMAKE_PLATFORM_DARWIN)
    message("Detected OSX x86_64")
  endif(CLR_CMAKE_PLATFORM_DARWIN)

  if(CLR_CMAKE_PLATFORM_FREEBSD)
    message("Detected FreeBSD amd64")
  endif(CLR_CMAKE_PLATFORM_FREEBSD)

  if(CLR_CMAKE_PLATFORM_NETBSD)
    message("Detected NetBSD amd64")
  endif(CLR_CMAKE_PLATFORM_NETBSD)
endif(CLR_CMAKE_PLATFORM_UNIX)

if (WIN32)
  add_definitions(-DPLATFORM_WINDOWS)

  # Define the CRT lib references that link into Desktop imports
  set(STATIC_MT_CRT_LIB  "libcmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
  set(STATIC_MT_VCRT_LIB  "libvcruntime$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
  set(STATIC_MT_CPP_LIB  "libcpmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
endif(WIN32)

# Architecture specific files folder name
if (CLR_CMAKE_TARGET_ARCH_AMD64)
    set(ARCH_SOURCES_DIR amd64)
elseif (CLR_CMAKE_TARGET_ARCH_ARM64)
    set(ARCH_SOURCES_DIR arm64)
elseif (CLR_CMAKE_TARGET_ARCH_ARM)
    set(ARCH_SOURCES_DIR arm)
elseif (CLR_CMAKE_TARGET_ARCH_I386)
    set(ARCH_SOURCES_DIR i386)
else ()
    clr_unknown_arch()
endif ()

#--------------------------------------
# Compile Options
#--------------------------------------
if (CLR_CMAKE_PLATFORM_UNIX)
  # Disable frame pointer optimizations so profilers can get better call stacks
  add_compile_options(-fno-omit-frame-pointer)

  # The -fms-extensions enable the stuff like __if_exists, __declspec(uuid()), etc.
  add_compile_options(-fms-extensions)
  #-fms-compatibility      Enable full Microsoft Visual C++ compatibility
  #-fms-extensions         Accept some non-standard constructs supported by the Microsoft compiler

  # Make signed arithmetic overflow of addition, subtraction, and multiplication wrap around
  # using twos-complement representation (this is normally undefined according to the C++ spec).
  add_compile_options(-fwrapv)

  if(CLR_CMAKE_PLATFORM_DARWIN)
    # We cannot enable "stack-protector-strong" on OS X due to a bug in clang compiler (current version 7.0.2)
    add_compile_options(-fstack-protector)
  else()
    check_cxx_compiler_flag(-fstack-protector-strong COMPILER_SUPPORTS_F_STACK_PROTECTOR_STRONG)
    if (COMPILER_SUPPORTS_F_STACK_PROTECTOR_STRONG)
      add_compile_options(-fstack-protector-strong)
    endif()
  endif(CLR_CMAKE_PLATFORM_DARWIN)

  # Contracts are disabled on UNIX.
  add_definitions(-DDISABLE_CONTRACTS)

  if (CLR_CMAKE_WARNINGS_ARE_ERRORS)
    # All warnings that are not explicitly disabled are reported as errors
    add_compile_options(-Werror)
  endif(CLR_CMAKE_WARNINGS_ARE_ERRORS)

  # Disabled common warnings
  add_compile_options(-Wno-unused-variable)
  add_compile_options(-Wno-unused-value)
  add_compile_options(-Wno-unused-function)

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
    # This warning is caused by comparing 'this' to NULL
    add_compile_options(-Wno-tautological-compare)
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
    check_cxx_compiler_flag(-faligned-new COMPILER_SUPPORTS_F_ALIGNED_NEW)
    if (COMPILER_SUPPORTS_F_ALIGNED_NEW)
      add_compile_options($<$<COMPILE_LANGUAGE:CXX>:-faligned-new>)
    endif()
  endif()

  # Some architectures (e.g., ARM) assume char type is unsigned while CoreCLR assumes char is signed
  # as x64 does. It has been causing issues in ARM (https://github.com/dotnet/coreclr/issues/4746)
  add_compile_options(-fsigned-char)

  # We mark the function which needs exporting with DLLEXPORT
  add_compile_options(-fvisibility=hidden)

  # Specify the minimum supported version of macOS
  if(CLR_CMAKE_PLATFORM_DARWIN)
    set(MACOS_VERSION_MIN_FLAGS -mmacosx-version-min=10.12)
    add_compile_options(${MACOS_VERSION_MIN_FLAGS})
    add_link_options(${MACOS_VERSION_MIN_FLAGS})
  endif(CLR_CMAKE_PLATFORM_DARWIN)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CLR_CMAKE_PLATFORM_UNIX_ARM)
   # Because we don't use CMAKE_C_COMPILER/CMAKE_CXX_COMPILER to use clang
   # we have to set the triple by adding a compiler argument
   add_compile_options(-mthumb)
   add_compile_options(-mfpu=vfpv3)
   add_compile_options(-march=armv7-a)
   if(ARM_SOFTFP)
     add_definitions(-DARM_SOFTFP)
     add_compile_options(-mfloat-abi=softfp)
   endif(ARM_SOFTFP)
endif(CLR_CMAKE_PLATFORM_UNIX_ARM)

if(CLR_CMAKE_PLATFORM_UNIX)
  add_compile_options(${CLR_ADDITIONAL_COMPILER_OPTIONS})
endif(CLR_CMAKE_PLATFORM_UNIX)

if (MSVC)
  # Compile options for targeting windows

  # The following options are set by the razzle build
  add_compile_options(/TP) # compile all files as C++
  add_compile_options(/d2Zi+) # make optimized builds debugging easier
  add_compile_options(/nologo) # Suppress Startup Banner
  add_compile_options(/W3) # set warning level to 3
  add_compile_options(/WX) # treat warnings as errors
  add_compile_options(/Oi) # enable intrinsics
  add_compile_options(/Oy-) # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
  add_compile_options(/U_MT) # undefine the predefined _MT macro
  add_compile_options(/GF) # enable read-only string pooling
  add_compile_options(/Gm-) # disable minimal rebuild
  add_compile_options(/EHa) # enable C++ EH (w/ SEH exceptions)
  add_compile_options(/Zp8) # pack structs on 8-byte boundary
  add_compile_options(/Gy) # separate functions for linker
  add_compile_options(/Zc:wchar_t-) # C++ language conformance: wchar_t is NOT the native type, but a typedef
  add_compile_options(/Zc:forScope) # C++ language conformance: enforce Standard C++ for scoping rules
  set(CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS} /GR-") # disable C++ RTTI
  add_compile_options(/FC) # use full pathnames in diagnostics
  add_compile_options(/MP) # Build with Multiple Processes (number of processes equal to the number of processors)
  add_compile_options(/GS) # Buffer Security Check
  add_compile_options(/Zm200) # Specify Precompiled Header Memory Allocation Limit of 150MB
  add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)
  add_compile_options(/Zi) # enable debugging information
  add_compile_options(/ZH:SHA_256) # use SHA256 for generating hashes of compiler processed source files.
  add_compile_options(/source-charset:utf-8) # Force MSVC to compile source as UTF-8.

  if (CLR_CMAKE_PLATFORM_ARCH_I386)
    add_compile_options(/Gz)
  endif (CLR_CMAKE_PLATFORM_ARCH_I386)

  add_compile_options($<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/GL>)
  add_compile_options($<$<OR:$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>,$<CONFIG:Checked>>:/O1>)

  if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  # The generator expression in the following command means that the /homeparams option is added only for debug builds
  add_compile_options($<$<CONFIG:Debug>:/homeparams>) # Force parameters passed in registers to be written to the stack
  endif (CLR_CMAKE_PLATFORM_ARCH_AMD64)

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

  add_compile_options($<$<OR:$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>,$<BOOL:$<TARGET_PROPERTY:DAC_COMPONENT>>>:/MT>)
  add_compile_options($<$<AND:$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>,$<NOT:$<BOOL:$<TARGET_PROPERTY:DAC_COMPONENT>>>>:/MTd>)

  add_compile_options($<$<COMPILE_LANGUAGE:ASM_MASM>:/ZH:SHA_256>)

  if (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)
    # Contracts work too slow on ARM/ARM64 DEBUG/CHECKED.
    add_definitions(-DDISABLE_CONTRACTS)
  endif (CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64)

endif (MSVC)

if(CLR_CMAKE_ENABLE_CODE_COVERAGE)

  if(CLR_CMAKE_PLATFORM_UNIX)
    string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
    if(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
      message( WARNING "Code coverage results with an optimised (non-Debug) build may be misleading" )
    endif(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)

    add_compile_options(-fprofile-arcs)
    add_compile_options(-ftest-coverage)
    add_link_options(--coverage)
  else()
    message(FATAL_ERROR "Code coverage builds not supported on current platform")
  endif(CLR_CMAKE_PLATFORM_UNIX)

endif(CLR_CMAKE_ENABLE_CODE_COVERAGE)
