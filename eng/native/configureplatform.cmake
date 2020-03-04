include(CheckPIESupported)

#----------------------------------------
# Detect and set platform variable names
#     - for non-windows build platform & architecture is detected using inbuilt CMAKE variables and cross target component configure
#     - for windows we use the passed in parameter to CMAKE to determine build arch
#----------------------------------------
set(CLR_CMAKE_HOST_OS ${CMAKE_SYSTEM_NAME})
if(CLR_CMAKE_HOST_OS STREQUAL Linux)
    set(CLR_CMAKE_HOST_UNIX 1)
    if(CLR_CROSS_COMPONENTS_BUILD)
        # CMAKE_HOST_SYSTEM_PROCESSOR returns the value of `uname -p` on host.
        if(CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL amd64)
            if(CLR_CMAKE_TARGET_ARCH STREQUAL "arm" OR CLR_CMAKE_TARGET_ARCH STREQUAL "armel")
                if(CMAKE_CROSSCOMPILING)
                    set(CLR_CMAKE_HOST_UNIX_X86 1)
                else()
                    set(CLR_CMAKE_HOST_UNIX_AMD64 1)
                endif()
            else()
                set(CLR_CMAKE_HOST_UNIX_AMD64 1)
            endif()
        elseif(CMAKE_HOST_SYSTEM_PROCESSOR STREQUAL i686)
            set(CLR_CMAKE_HOST_UNIX_X86 1)
        else()
            clr_unknown_arch()
        endif()
    else()
        # CMAKE_SYSTEM_PROCESSOR returns the value of `uname -p` on target.
        # For the AMD/Intel 64bit architecture two different strings are common.
        # Linux and Darwin identify it as "x86_64" while FreeBSD and netbsd uses the
        # "amd64" string. Accept either of the two here.
        if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64)
            set(CLR_CMAKE_HOST_UNIX_AMD64 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l)
            set(CLR_CMAKE_HOST_UNIX_ARM 1)
            set(CLR_CMAKE_HOST_UNIX_ARMV7L 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL arm)
            set(CLR_CMAKE_HOST_UNIX_ARM 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
            set(CLR_CMAKE_HOST_UNIX_ARM64 1)
        elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL i686)
            set(CLR_CMAKE_HOST_UNIX_X86 1)
        else()
            clr_unknown_arch()
        endif()
    endif()
    set(CLR_CMAKE_HOST_LINUX 1)

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
            set(CLR_CMAKE_HOST_OS ${CLR_CMAKE_LINUX_ID})
        elseif(CLR_CMAKE_LINUX_ID STREQUAL alpine)
            set(CLR_CMAKE_HOST_ALPINE_LINUX 1)
            set(CLR_CMAKE_HOST_OS ${CLR_CMAKE_LINUX_ID})
        elseif(CLR_CMAKE_LINUX_ID STREQUAL android)
            set(CLR_CMAKE_HOST_ANDROID 1)
            set(CLR_CMAKE_HOST_OS ${CLR_CMAKE_LINUX_ID})
        endif()
    endif(DEFINED CLR_CMAKE_LINUX_ID)
endif(CLR_CMAKE_HOST_OS STREQUAL Linux)

if(CLR_CMAKE_HOST_OS STREQUAL Darwin)
    set(CLR_CMAKE_HOST_UNIX 1)
    set(CLR_CMAKE_HOST_UNIX_AMD64 1)
    set(CLR_CMAKE_HOST_DARWIN 1)
    set(CMAKE_ASM_COMPILE_OBJECT "${CMAKE_C_COMPILER} <FLAGS> <DEFINES> <INCLUDES> -o <OBJECT> -c <SOURCE>")
endif(CLR_CMAKE_HOST_OS STREQUAL Darwin)

if(CLR_CMAKE_HOST_OS STREQUAL FreeBSD)
    set(CLR_CMAKE_HOST_UNIX 1)
    set(CLR_CMAKE_HOST_UNIX_AMD64 1)
    set(CLR_CMAKE_HOST_FREEBSD 1)
endif(CLR_CMAKE_HOST_OS STREQUAL FreeBSD)

if(CLR_CMAKE_HOST_OS STREQUAL OpenBSD)
    set(CLR_CMAKE_HOST_UNIX 1)
    set(CLR_CMAKE_HOST_UNIX_AMD64 1)
    set(CLR_CMAKE_HOST_OPENBSD 1)
endif(CLR_CMAKE_HOST_OS STREQUAL OpenBSD)

if(CLR_CMAKE_HOST_OS STREQUAL NetBSD)
    set(CLR_CMAKE_HOST_UNIX 1)
    set(CLR_CMAKE_HOST_UNIX_AMD64 1)
    set(CLR_CMAKE_HOST_NETBSD 1)
endif(CLR_CMAKE_HOST_OS STREQUAL NetBSD)

if(CLR_CMAKE_HOST_OS STREQUAL SunOS)
    set(CLR_CMAKE_HOST_UNIX 1)
    EXECUTE_PROCESS(
        COMMAND isainfo -n
        OUTPUT_VARIABLE SUNOS_NATIVE_INSTRUCTION_SET)

    if(SUNOS_NATIVE_INSTRUCTION_SET MATCHES "amd64")
        set(CLR_CMAKE_HOST_UNIX_AMD64 1)
        set(CMAKE_SYSTEM_PROCESSOR "amd64")
    else()
        clr_unknown_arch()
    endif()

    set(CLR_CMAKE_HOST_SUNOS 1)
endif(CLR_CMAKE_HOST_OS STREQUAL SunOS)

if(CLR_CMAKE_HOST_OS STREQUAL Windows)
    set(CLR_CMAKE_HOST_OS Windows_NT)
    set(CLR_CMAKE_HOST_WIN32 1)
endif(CLR_CMAKE_HOST_OS STREQUAL Windows)

if(CLR_CMAKE_HOST_OS STREQUAL Emscripten)
    set(CLR_CMAKE_HOST_ARCH_WASM 1)
endif(CLR_CMAKE_HOST_OS STREQUAL Emscripten)

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
if(CLR_CMAKE_HOST_UNIX_ARM)
    set(CLR_CMAKE_HOST_ARCH_ARM 1)
    set(CLR_CMAKE_HOST_ARCH "arm")

    if(CLR_CMAKE_HOST_UNIX_ARMV7L)
        set(CLR_CMAKE_HOST_ARCH_ARMV7L 1)
    endif()
elseif(CLR_CMAKE_HOST_UNIX_ARM64)
    set(CLR_CMAKE_HOST_ARCH_ARM64 1)
    set(CLR_CMAKE_HOST_ARCH "arm64")
elseif(CLR_CMAKE_HOST_UNIX_AMD64)
    set(CLR_CMAKE_HOST_ARCH_AMD64 1)
    set(CLR_CMAKE_HOST_ARCH "x64")
elseif(CLR_CMAKE_HOST_UNIX_X86)
    set(CLR_CMAKE_HOST_ARCH_I386 1)
    set(CLR_CMAKE_HOST_ARCH "x86")
elseif(WIN32)
    # CLR_CMAKE_HOST_ARCH is passed in as param to cmake
    if (CLR_CMAKE_HOST_ARCH STREQUAL x64)
        set(CLR_CMAKE_HOST_ARCH_AMD64 1)
    elseif(CLR_CMAKE_HOST_ARCH STREQUAL x86)
        set(CLR_CMAKE_HOST_ARCH_I386 1)
    elseif(CLR_CMAKE_HOST_ARCH STREQUAL arm)
        set(CLR_CMAKE_HOST_ARCH_ARM 1)
    elseif(CLR_CMAKE_HOST_ARCH STREQUAL arm64)
        set(CLR_CMAKE_HOST_ARCH_ARM64 1)
    else()
        clr_unknown_arch()
    endif()
endif()

# Set TARGET architecture variables
# Target arch will be a cmake param (optional) for both windows as well as non-windows build
# if target arch is not specified then host & target are same
if(NOT DEFINED CLR_CMAKE_TARGET_ARCH OR CLR_CMAKE_TARGET_ARCH STREQUAL "" )
  set(CLR_CMAKE_TARGET_ARCH ${CLR_CMAKE_HOST_ARCH})

  # This is required for "arm" targets (CMAKE_SYSTEM_PROCESSOR "armv7l"),
  # for which this flag otherwise won't be set up below
  if (CLR_CMAKE_HOST_ARCH_ARMV7L)
    set(CLR_CMAKE_TARGET_ARCH_ARMV7L 1)
  endif()
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
    set(CLR_CMAKE_TARGET_ARCH_ARMV7L 1)
    set(ARM_SOFTFP 1)
  else()
    clr_unknown_arch()
endif()

# Set TARGET architecture variables
# Target os will be a cmake param (optional) for both windows as well as non-windows build
# if target os is not specified then host & target os are same
if (NOT DEFINED CLR_CMAKE_TARGET_OS OR CLR_CMAKE_TARGET_OS STREQUAL "" )
  set(CLR_CMAKE_TARGET_OS ${CLR_CMAKE_HOST_OS})
endif()

if(CLR_CMAKE_TARGET_OS STREQUAL Linux)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_LINUX 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL Linux)

if(CLR_CMAKE_TARGET_OS STREQUAL tizen)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_LINUX 1)
    set(CLR_CMAKE_TARGET_TIZEN_LINUX 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL tizen)

if(CLR_CMAKE_TARGET_OS STREQUAL alpine)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_LINUX 1)
    set(CLR_CMAKE_TARGET_ALPINE_LINUX 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL alpine)

if(CLR_CMAKE_TARGET_OS STREQUAL android)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_LINUX 1)
    set(CLR_CMAKE_TARGET_ANDROID 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL android)

if(CLR_CMAKE_TARGET_OS STREQUAL Darwin)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_DARWIN 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL Darwin)

if(CLR_CMAKE_TARGET_OS STREQUAL FreeBSD)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_FREEBSD 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL FreeBSD)

if(CLR_CMAKE_TARGET_OS STREQUAL OpenBSD)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_OPENBSD 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL OpenBSD)

if(CLR_CMAKE_TARGET_OS STREQUAL NetBSD)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_NETBSD 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL NetBSD)

if(CLR_CMAKE_TARGET_OS STREQUAL SunOS)
    set(CLR_CMAKE_TARGET_UNIX 1)
    set(CLR_CMAKE_TARGET_SUNOS 1)
endif(CLR_CMAKE_TARGET_OS STREQUAL SunOS)

if(CLR_CMAKE_TARGET_UNIX)
    if(CLR_CMAKE_TARGET_ARCH STREQUAL x64)
        set(CLR_CMAKE_TARGET_UNIX_AMD64 1)
    elseif(CLR_CMAKE_TARGET_ARCH STREQUAL armel)
        set(CLR_CMAKE_TARGET_UNIX_ARM 1)
    elseif(CLR_CMAKE_TARGET_ARCH STREQUAL arm)
        set(CLR_CMAKE_TARGET_UNIX_ARM 1)
    elseif(CLR_CMAKE_TARGET_ARCH STREQUAL arm64)
        set(CLR_CMAKE_TARGET_UNIX_ARM64 1)
    elseif(CLR_CMAKE_TARGET_ARCH STREQUAL x86)
        set(CLR_CMAKE_TARGET_UNIX_X86 1)
    else()
        clr_unknown_arch()
    endif()
else()
    set(CLR_CMAKE_TARGET_WIN32 1)
endif(CLR_CMAKE_TARGET_UNIX)

# check if host & target os/arch combination are valid
if (CLR_CMAKE_TARGET_OS STREQUAL CLR_CMAKE_HOST_OS)
    if(NOT(CLR_CMAKE_TARGET_ARCH STREQUAL CLR_CMAKE_HOST_ARCH))
        if(NOT((CLR_CMAKE_HOST_ARCH_AMD64 AND CLR_CMAKE_TARGET_ARCH_ARM64) OR (CLR_CMAKE_HOST_ARCH_I386 AND CLR_CMAKE_TARGET_ARCH_ARM) OR (CLR_CMAKE_HOST_ARCH_AMD64 AND CLR_CMAKE_TARGET_ARCH_ARM)))
            message(FATAL_ERROR "Invalid platform and target arch combination")
        endif()
    endif()
else()
    if(NOT (CLR_CMAKE_HOST_OS STREQUAL Windows_NT))
        message(FATAL_ERROR "Invalid host and target os/arch combination. Host OS: ${CLR_CMAKE_HOST_OS}")
    endif()
    if(NOT (CLR_CMAKE_TARGET_LINUX OR CLR_CMAKE_TARGET_ALPINE_LINUX))
        message(FATAL_ERROR "Invalid host and target os/arch combination. Target OS: ${CLR_CMAKE_TARGET_OS}")
    endif()
    if(NOT ((CLR_CMAKE_HOST_ARCH_AMD64 AND (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64)) OR (CLR_CMAKE_HOST_ARCH_I386 AND CLR_CMAKE_TARGET_ARCH_ARM)))
        message(FATAL_ERROR "Invalid host and target os/arch combination. Host Arch: ${CLR_CMAKE_HOST_ARCH} Target Arch: ${CLR_CMAKE_TARGET_ARCH}")
    endif()
endif()

if(NOT CLR_CMAKE_HOST_ARCH_WASM)
    # All code we build should be compiled as position independent
    get_property(languages GLOBAL PROPERTY ENABLED_LANGUAGES)
    if("CXX" IN_LIST languages)
        set(CLR_PIE_LANGUAGE CXX)
    else()
        set(CLR_PIE_LANGUAGE C)
    endif()
    check_pie_supported(OUTPUT_VARIABLE PIE_SUPPORT_OUTPUT LANGUAGES ${CLR_PIE_LANGUAGE})
    if(NOT MSVC AND NOT CMAKE_${CLR_PIE_LANGUAGE}_LINK_PIE_SUPPORTED)
        message(WARNING "PIE is not supported at link time: ${PIE_SUPPORT_OUTPUT}.\n"
                  "PIE link options will not be passed to linker.")
    endif()
    set(CMAKE_POSITION_INDEPENDENT_CODE ON)
endif(NOT CLR_CMAKE_HOST_ARCH_WASM)
