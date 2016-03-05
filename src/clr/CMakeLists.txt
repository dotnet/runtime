# Require at least version 2.8.12 of CMake
cmake_minimum_required(VERSION 2.8.12)

# Set the project name
project(CoreCLR)

set(CORECLR_SET_RPATH ON)

if(CORECLR_SET_RPATH)
    # Enable @rpath support for shared libraries.
    set(MACOSX_RPATH ON)
endif(CORECLR_SET_RPATH)

if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
    cmake_policy(SET CMP0042 NEW)
endif()

set(CLR_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(VM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/src/vm)
set(GENERATED_INCLUDE_DIR ${CMAKE_CURRENT_BINARY_DIR}/src/inc)

function(clr_unknown_arch)
    if (WIN32)
        message(FATAL_ERROR "Only AMD64, ARM64 and I386 are supported")
    else()
        message(FATAL_ERROR "Only AMD64, ARM64 and ARM are supported")
    endif()
endfunction()

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64)
        set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
    elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL armv7l)
        set(CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM 1)
        # Because we don't use CMAKE_C_COMPILER/CMAKE_CXX_COMPILER to use clang
        # we have to set the triple by adding a compiler argument
        add_compile_options(-target armv7-linux-gnueabihf)
        add_compile_options(-mthumb)
        add_compile_options(-mfpu=vfpv3)
    elseif(CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
        set(CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM64 1)
    else()
        clr_unknown_arch()
    endif()
    set(CLR_CMAKE_PLATFORM_LINUX 1)
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
  set(CLR_CMAKE_PLATFORM_DARWIN 1)
  if(CMAKE_VERSION VERSION_LESS "3.4.0")
    set(CMAKE_ASM_COMPILE_OBJECT "${CMAKE_C_COMPILER} <FLAGS> <DEFINES> -o <OBJECT> -c <SOURCE>")
  else()
    set(CMAKE_ASM_COMPILE_OBJECT "${CMAKE_C_COMPILER} <FLAGS> <DEFINES> <INCLUDES> -o <OBJECT> -c <SOURCE>")
  endif(CMAKE_VERSION VERSION_LESS "3.4.0")
endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

if(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
  set(CLR_CMAKE_PLATFORM_FREEBSD 1)
endif(CMAKE_SYSTEM_NAME STREQUAL FreeBSD)

if(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
  set(CLR_CMAKE_PLATFORM_OPENBSD 1)
endif(CMAKE_SYSTEM_NAME STREQUAL OpenBSD)

if(CMAKE_SYSTEM_NAME STREQUAL NetBSD)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
  set(CLR_CMAKE_PLATFORM_NETBSD 1)
endif(CMAKE_SYSTEM_NAME STREQUAL NetBSD)

if(CMAKE_SYSTEM_NAME STREQUAL SunOS)
  set(CLR_CMAKE_PLATFORM_UNIX 1)
  EXECUTE_PROCESS(
    COMMAND isainfo -n
    OUTPUT_VARIABLE SUNOS_NATIVE_INSTRUCTION_SET
    )
  if(SUNOS_NATIVE_INSTRUCTION_SET MATCHES "amd64")
    set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
    set(CMAKE_SYSTEM_PROCESSOR "amd64")
  else()
    clr_unknown_arch()
  endif()
  set(CLR_CMAKE_PLATFORM_SUNOS 1)
endif(CMAKE_SYSTEM_NAME STREQUAL SunOS)

if(CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM)
  set(CLR_CMAKE_PLATFORM_ARCH_ARM 1)
elseif(CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM64)
  set(CLR_CMAKE_PLATFORM_ARCH_ARM64 1)
elseif(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
  set(CLR_CMAKE_PLATFORM_ARCH_AMD64 1)
elseif(WIN32)
  if (CLR_CMAKE_TARGET_ARCH STREQUAL x64)
    set(CLR_CMAKE_PLATFORM_ARCH_AMD64 1)
    set(IS_64BIT_BUILD 1)
  elseif(CLR_CMAKE_TARGET_ARCH STREQUAL x86)
    set(CLR_CMAKE_PLATFORM_ARCH_I386 1)
    set(IS_64BIT_BUILD 0)
  elseif(CLR_CMAKE_TARGET_ARCH STREQUAL arm64)
    set(CLR_CMAKE_PLATFORM_ARCH_ARM64 1)
    set(IS_64BIT_BUILD 1)

    # CMAKE_CXX_COMPILER will default to the compiler installed with
    # Visual studio. Overwrite it to the compiler on the path.
    # TODO, remove when cmake generator supports Arm64 as a target.

    find_program(PATH_CXX_COMPILER cl)
    set(CMAKE_CXX_COMPILER ${PATH_CXX_COMPILER})

    message("Overwriting the CMAKE_CXX_COMPILER.")
    message(CMAKE_CXX_COMPILER found:${CMAKE_CXX_COMPILER})
  else()
    clr_unknown_arch()
  endif()
endif()

if(CLR_CMAKE_PLATFORM_UNIX)
  # Set flag to indicate if this will be a 64bit build
  # CMAKE_SYSTEM_PROCESSOR returns the value of `uname -p`.
  # For the AMD/Intel 64bit architecure two different strings are common.
  # Linux and Darwin identify it as "x86_64" while FreeBSD uses the
  # "amd64" string. Accept either of the two here.
  if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
    set(IS_64BIT_BUILD 1)
  endif (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL amd64 OR CMAKE_SYSTEM_PROCESSOR STREQUAL aarch64)
endif(CLR_CMAKE_PLATFORM_UNIX)

# Ensure that python is present
find_program(PYTHON python)
if (PYTHON STREQUAL "PYTHON-NOTFOUND")
    message(FATAL_ERROR "PYTHON not found: Please install Python 2.7.9 or later from https://www.python.org/downloads/")
endif()

if(WIN32)
    enable_language(ASM_MASM)

    # Ensure that MC is present
    find_program(MC mc)
    if (MC STREQUAL "MC-NOTFOUND")
        message(FATAL_ERROR "MC not found")
    endif()
else()
    enable_language(ASM)

    # Ensure that awk is present
    find_program(AWK awk)
    if (AWK STREQUAL "AWK-NOTFOUND")
        message(FATAL_ERROR "AWK not found")
    endif()
endif(WIN32)

# Build a list of compiler definitions by putting -D in front of each define.
function(get_compile_definitions DefinitionName)
    # Get the current list of definitions
    get_directory_property(COMPILE_DEFINITIONS_LIST COMPILE_DEFINITIONS)

    foreach(DEFINITION IN LISTS COMPILE_DEFINITIONS_LIST)
        if (${DEFINITION} MATCHES "^\\$<\\$<CONFIG:([^>]+)>:([^>]+)>$")
            # The entries that contain generator expressions must have the -D inside of the
            # expression. So we transform e.g. $<$<CONFIG:Debug>:_DEBUG> to $<$<CONFIG:Debug>:-D_DEBUG>
            set(DEFINITION "$<$<CONFIG:${CMAKE_MATCH_1}>:-D${CMAKE_MATCH_2}>")
        else()
            set(DEFINITION -D${DEFINITION})
        endif()
        list(APPEND DEFINITIONS ${DEFINITION})
    endforeach()
    set(${DefinitionName} ${DEFINITIONS} PARENT_SCOPE)
endfunction(get_compile_definitions)

# Build a list of include directories by putting -I in front of each include dir.
function(get_include_directories IncludeDirectories)
    get_directory_property(dirs INCLUDE_DIRECTORIES)
    foreach(dir IN LISTS dirs)
        list(APPEND INC_DIRECTORIES -I${dir})
    endforeach()
    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories)

# Set the passed in RetSources variable to the list of sources with added current source directory
# to form absolute paths.
# The parameters after the RetSources are the input files.
function(convert_to_absolute_path RetSources)
    set(Sources ${ARGN})
    foreach(Source IN LISTS Sources)
        list(APPEND AbsolutePathSources ${CMAKE_CURRENT_SOURCE_DIR}/${Source})
    endforeach()
    set(${RetSources} ${AbsolutePathSources} PARENT_SCOPE)
endfunction(convert_to_absolute_path)

#Preprocess exports definition file
function(preprocess_def_file inputFilename outputFilename)
  get_compile_definitions(PREPROCESS_DEFINITIONS)

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${CMAKE_CXX_COMPILER} /P /EP /TC ${PREPROCESS_DEFINITIONS}  /Fi${outputFilename}  ${inputFilename}
    DEPENDS ${inputFilename}
    COMMENT "Preprocessing ${inputFilename}"
  )

  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

function(generate_exports_file inputFilename outputFilename)

  if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(AWK_SCRIPT generateexportedsymbols.awk)
  else()
    set(AWK_SCRIPT generateversionscript.awk)
  endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${AWK} -f ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT} ${inputFilename} >${outputFilename}
    DEPENDS ${inputFilename} ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT}
    COMMENT "Generating exports file ${outputFilename}"
  )
  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

function(add_precompiled_header header cppFile targetSources)
  if(MSVC)
    set(precompiledBinary "${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/stdafx.pch")

    set_source_files_properties(${cppFile}
                                PROPERTIES COMPILE_FLAGS "/Yc\"${header}\" /Fp\"${precompiledBinary}\""
                                           OBJECT_OUTPUTS "${precompiledBinary}")
    set_source_files_properties(${${targetSources}}
                                PROPERTIES COMPILE_FLAGS "/Yu\"${header}\" /Fp\"${precompiledBinary}\""
                                           OBJECT_DEPENDS "${precompiledBinary}")
    # Add cppFile to SourcesVar
    set(${targetSources} ${${targetSources}} ${cppFile} PARENT_SCOPE)
  endif(MSVC)
endfunction()

# Includes

if (CMAKE_CONFIGURATION_TYPES) # multi-configuration generator?
    set(CMAKE_CONFIGURATION_TYPES "Debug;Checked;Release;RelWithDebInfo" CACHE STRING "" FORCE)
endif (CMAKE_CONFIGURATION_TYPES)

set(CMAKE_C_FLAGS_CHECKED ${CLR_C_FLAGS_CHECKED_INIT} CACHE STRING "Flags used by the compiler during checked builds.")
set(CMAKE_CXX_FLAGS_CHECKED ${CLR_CXX_FLAGS_CHECKED_INIT} CACHE STRING "Flags used by the compiler during checked builds.")
set(CMAKE_EXE_LINKER_FLAGS_CHECKED "")
set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "")

if (WIN32)
  # For multi-configuration toolset (as Visual Studio)
  # set the different configuration defines.
  foreach (Config DEBUG CHECKED RELEASE RELWITHDEBINFO)
    foreach (Definition IN LISTS CLR_DEFINES_${Config}_INIT)
      set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS $<$<CONFIG:${Config}>:${Definition}>)
    endforeach (Definition)
  endforeach (Config)

elseif (CLR_CMAKE_PLATFORM_UNIX)
  # Set the values to display when interactively configuring CMAKE_BUILD_TYPE
  set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "DEBUG;CHECKED;RELEASE;RELWITHDEBINFO")

  # Use uppercase CMAKE_BUILD_TYPE for the string comparisons below
  string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)

  # For single-configuration toolset
  # set the different configuration defines.
  if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    # First DEBUG
    set_property(DIRECTORY  PROPERTY COMPILE_DEFINITIONS ${CLR_DEFINES_DEBUG_INIT})
  elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
    # Then CHECKED
    set_property(DIRECTORY PROPERTY COMPILE_DEFINITIONS ${CLR_DEFINES_CHECKED_INIT})
  elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
    # Then RELEASE
    set_property(DIRECTORY PROPERTY COMPILE_DEFINITIONS ${CLR_DEFINES_RELEASE_INIT})
  elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
    # And then RELWITHDEBINFO
    set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS ${CLR_DEFINES_RELWITHDEBINFO_INIT})
  else ()
    message(FATAL_ERROR "Unknown build type! Set CMAKE_BUILD_TYPE to DEBUG, CHECKED, RELEASE, or RELWITHDEBINFO!")
  endif ()

endif(WIN32)

if (CLR_CMAKE_PLATFORM_UNIX)
  add_definitions(-DPLATFORM_UNIX=1)

  if(CLR_CMAKE_PLATFORM_LINUX)
    if(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
      message("Detected Linux x86_64")
      add_definitions(-DLINUX64)
    elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
      message("Detected Linux ARM")
      add_definitions(-DLINUX32)
    elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
      message("Detected Linux ARM64")
      add_definitions(-DLINUX64)
    else()
      clr_unknown_arch()
    endif()
  endif(CLR_CMAKE_PLATFORM_LINUX)

  if(CLR_CMAKE_PLATFORM_DARWIN)
    message("Detected OSX x86_64")
  endif(CLR_CMAKE_PLATFORM_DARWIN)

  if(CLR_CMAKE_PLATFORM_FREEBSD)
    message("Detected FreeBSD amd64")
  endif(CLR_CMAKE_PLATFORM_FREEBSD)

  # Disable frame pointer optimizations so profilers can get better call stacks
  add_compile_options(-fno-omit-frame-pointer)

  # The -fms-extensions enable the stuff like __if_exists, __declspec(uuid()), etc.
  add_compile_options(-fms-extensions )
  #-fms-compatibility      Enable full Microsoft Visual C++ compatibility
  #-fms-extensions         Accept some non-standard constructs supported by the Microsoft compiler

  if(CLR_CMAKE_PLATFORM_DARWIN)
    # We cannot enable "stack-protector-strong" on OS X due to a bug in clang compiler (current version 7.0.2)
    add_compile_options(-fstack-protector)
  else()
    add_compile_options(-fstack-protector-strong)
  endif(CLR_CMAKE_PLATFORM_DARWIN)

  # set the CLANG sanitizer flags for debug build
  if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
    # obtain settings from running enablesanitizers.sh
    string(FIND "$ENV{DEBUG_SANITIZERS}" "asan" __ASAN_POS)
    string(FIND "$ENV{DEBUG_SANITIZERS}" "ubsan" __UBSAN_POS)
    if ((${__ASAN_POS} GREATER -1) OR (${__UBSAN_POS} GREATER -1))
      set(CLR_SANITIZE_CXX_FLAGS "${CLR_SANITIZE_CXX_FLAGS} -fsanitize-blacklist=${CMAKE_CURRENT_SOURCE_DIR}/sanitizerblacklist.txt -fsanitize=")
      set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS} -fsanitize=")
      if (${__ASAN_POS} GREATER -1)
        set(CLR_SANITIZE_CXX_FLAGS "${CLR_SANITIZE_CXX_FLAGS}address,")
        set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}address,")
        message("Address Sanitizer (asan) enabled")
      endif ()
      if (${__UBSAN_POS} GREATER -1)
        # all sanitizier flags are enabled except alignment (due to heavy use of __unaligned modifier)
        set(CLR_SANITIZE_CXX_FLAGS "${CLR_SANITIZE_CXX_FLAGS}bool,bounds,enum,float-cast-overflow,float-divide-by-zero,function,integer,nonnull-attribute,null,object-size,return,returns-nonnull-attribute,shift,unreachable,vla-bound,vptr")
        set(CLR_SANITIZE_LINK_FLAGS "${CLR_SANITIZE_LINK_FLAGS}undefined")
        message("Undefined Behavior Sanitizer (ubsan) enabled")
      endif ()

      # -fdata-sections -ffunction-sections: each function has own section instead of one per .o file (needed for --gc-sections)
      # -fPIC: enable Position Independent Code normally just for shared libraries but required when linking with address sanitizer
      # -O1: optimization level used instead of -O0 to avoid compile error "invalid operand for inline asm constraint"
      set(CMAKE_CXX_FLAGS_DEBUG "${CMAKE_CXX_FLAGS_DEBUG} ${CLR_SANITIZE_CXX_FLAGS} -fdata-sections -ffunction-sections -fPIC -O1")
      set(CMAKE_CXX_FLAGS_CHECKED "${CMAKE_CXX_FLAGS_CHECKED} ${CLR_SANITIZE_CXX_FLAGS} -fdata-sections -ffunction-sections -fPIC -O1")

      set(CMAKE_EXE_LINKER_FLAGS_DEBUG "${CMAKE_EXE_LINKER_FLAGS_DEBUG} ${CLR_SANITIZE_LINK_FLAGS}")
      set(CMAKE_EXE_LINKER_FLAGS_CHECKED "${CMAKE_EXE_LINKER_FLAGS_CHECKED} ${CLR_SANITIZE_LINK_FLAGS}")

      # -Wl and --gc-sections: drop unused sections\functions (similar to Windows /Gy function-level-linking)
      set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "${CMAKE_SHARED_LINKER_FLAGS_DEBUG} ${CLR_SANITIZE_LINK_FLAGS} -Wl,--gc-sections")
      set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "${CMAKE_SHARED_LINKER_FLAGS_CHECKED} ${CLR_SANITIZE_LINK_FLAGS} -Wl,--gc-sections")
    endif ()
  endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)

  add_definitions(-DDISABLE_CONTRACTS)
  # The -ferror-limit is helpful during the porting, it makes sure the compiler doesn't stop
  # after hitting just about 20 errors.
  add_compile_options(-ferror-limit=4096)

  # All warnings that are not explicitly disabled are reported as errors
  add_compile_options(-Werror)

  # Disabled warnings
  add_compile_options(-Wno-unused-private-field)
  add_compile_options(-Wno-unused-variable)
  # Explicit constructor calls are not supported by clang (this->ClassName::ClassName())
  add_compile_options(-Wno-microsoft)
  # This warning is caused by comparing 'this' to NULL
  add_compile_options(-Wno-tautological-compare)
  # There are constants of type BOOL used in a condition. But BOOL is defined as int
  # and so the compiler thinks that there is a mistake.
  add_compile_options(-Wno-constant-logical-operand)

  add_compile_options(-Wno-unknown-warning-option)

  #These seem to indicate real issues
  add_compile_options(-Wno-invalid-offsetof)
  # The following warning indicates that an attribute __attribute__((__ms_struct__)) was applied
  # to a struct or a class that has virtual members or a base class. In that case, clang
  # may not generate the same object layout as MSVC.
  add_compile_options(-Wno-incompatible-ms-struct)

endif(CLR_CMAKE_PLATFORM_UNIX)

if (WIN32)
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
  add_compile_options(/GR-) # disable C++ RTTI
  add_compile_options(/FC) # use full pathnames in diagnostics
  add_compile_options(/MP) # Build with Multiple Processes (number of processes equal to the number of processors)
  add_compile_options(/GS) # Buffer Security Check
  add_compile_options(/Zm200) # Specify Precompiled Header Memory Allocation Limit of 150MB
  add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)
  add_compile_options(/Zi) # enable debugging information

  if (CLR_CMAKE_PLATFORM_ARCH_I386)
    add_compile_options(/Gz)
  endif (CLR_CMAKE_PLATFORM_ARCH_I386)

  add_compile_options($<$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>:/GL>)
  add_compile_options($<$<OR:$<OR:$<CONFIG:Release>,$<CONFIG:Relwithdebinfo>>,$<CONFIG:Checked>>:/O1>)

  if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  # The generator expression in the following command means that the /homeparams option is added only for debug builds
  add_compile_options($<$<CONFIG:Debug>:/homeparams>) # Force parameters passed in registers to be written to the stack
  endif (CLR_CMAKE_PLATFORM_ARCH_AMD64)

  if(NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
    # enable control-flow-guard support for native components for non-Arm64 builds
    add_compile_options(/guard:cf) 
    set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /guard:cf")
    set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /guard:cf")
  endif (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)

  # Incremental linking with CFG is broken until next VS release.
  # This needs to be appended to the last for each build type to override the default flag.
  set(NO_INCREMENTAL_LINKER_FLAGS "/INCREMENTAL:NO")

  # Linker flags
  #
  # Disable the following line for UNIX altjit on Windows
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /MANIFEST:NO") #Do not create Side-by-Side Assembly Manifest
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SUBSYSTEM:WINDOWS,6.00") #windows subsystem
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /LARGEADDRESSAWARE") # can handle addresses larger than 2 gigabytes
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /RELEASE") #sets the checksum in the header
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NXCOMPAT") #Compatible with Data Execution Prevention
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DYNAMICBASE") #Use address space layout randomization
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUGTYPE:cv,fixup") #debugging format
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /PDBCOMPRESS") #shrink pdb size
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG")
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /IGNORE:4197,4013,4254,4070,4221")

  set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /IGNORE:4221")

  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /PDBCOMPRESS")
  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /STACK:1572864")

  # Debug build specific flags
  set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "/NOVCFEATURE ${NO_INCREMENTAL_LINKER_FLAGS}")
  set(CMAKE_EXE_LINKER_FLAGS_DEBUG "${NO_INCREMENTAL_LINKER_FLAGS}")

  # Checked build specific flags
  set(CMAKE_SHARED_LINKER_FLAGS_CHECKED "${CMAKE_SHARED_LINKER_FLAGS_CHECKED} /OPT:REF /OPT:NOICF /NOVCFEATURE ${NO_INCREMENTAL_LINKER_FLAGS}")
  set(CMAKE_STATIC_LINKER_FLAGS_CHECKED "${CMAKE_STATIC_LINKER_FLAGS_CHECKED}")
  set(CMAKE_EXE_LINKER_FLAGS_CHECKED "${CMAKE_EXE_LINKER_FLAGS_CHECKED} /OPT:REF /OPT:NOICF ${NO_INCREMENTAL_LINKER_FLAGS}")

  # Release build specific flags
  set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")
  set(CMAKE_STATIC_LINKER_FLAGS_RELEASE "${CMAKE_STATIC_LINKER_FLAGS_RELEASE} /LTCG")
  set(CMAKE_EXE_LINKER_FLAGS_RELEASE "${CMAKE_EXE_LINKER_FLAGS_RELEASE} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")

  # ReleaseWithDebugInfo build specific flags
  set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")
  set(CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_STATIC_LINKER_FLAGS_RELWITHDEBINFO} /LTCG")
  set(CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_EXE_LINKER_FLAGS_RELWITHDEBINFO} /LTCG /OPT:REF /OPT:ICF ${NO_INCREMENTAL_LINKER_FLAGS}")

# Temporary until cmake has VS generators for arm64
if(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /machine:arm64")
  set(CMAKE_STATIC_LINKER_FLAGS "${CMAKE_STATIC_LINKER_FLAGS} /machine:arm64")
  set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /machine:arm64")
endif(CLR_CMAKE_PLATFORM_ARCH_ARM64)

endif (WIN32)

OPTION(CMAKE_ENABLE_CODE_COVERAGE "Enable code coverage" OFF)

if(CMAKE_ENABLE_CODE_COVERAGE)

  if(CLR_CMAKE_PLATFORM_UNIX)
    string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)
    if(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
      message( WARNING "Code coverage results with an optimised (non-Debug) build may be misleading" )
    endif(NOT UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)

    add_compile_options(-fprofile-arcs)
    add_compile_options(-ftest-coverage)
    set(CLANG_COVERAGE_LINK_FLAGS  "--coverage")
    set(CMAKE_SHARED_LINKER_FLAGS  "${CMAKE_SHARED_LINKER_FLAGS} ${CLANG_COVERAGE_LINK_FLAGS}")
    set(CMAKE_EXE_LINKER_FLAGS     "${CMAKE_EXE_LINKER_FLAGS} ${CLANG_COVERAGE_LINK_FLAGS}")
  else()
    message(FATAL_ERROR "Code coverage builds not supported on current platform")
  endif(CLR_CMAKE_PLATFORM_UNIX)

endif(CMAKE_ENABLE_CODE_COVERAGE)

# Start of projects that require usage of platform include files

if(CLR_CMAKE_PLATFORM_UNIX)
  add_subdirectory(src/corefx)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(IS_64BIT_BUILD)
  add_definitions(-DBIT64=1)
endif(IS_64BIT_BUILD)

if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  if (CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
    add_definitions(-DDBG_TARGET_AMD64_UNIX)
  endif()
  add_definitions(-D_TARGET_AMD64_=1)
  add_definitions(-DDBG_TARGET_AMD64)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM64)
  if (CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM64)
    add_definitions(-DDBG_TARGET_ARM64_UNIX)
  endif()
  add_definitions(-D_TARGET_ARM64_=1)
  add_definitions(-DDBG_TARGET_ARM64)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM)
  if (CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM)
    add_definitions(-DDBG_TARGET_ARM_UNIX)
  endif (CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM)
  add_definitions(-D_TARGET_ARM_=1)
  add_definitions(-DDBG_TARGET_ARM)
elseif (CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-D_TARGET_X86_=1)
  add_definitions(-DDBG_TARGET_X86)
else ()
  clr_unknown_arch()
endif (CLR_CMAKE_PLATFORM_ARCH_AMD64)

if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-D_AMD64_)
  add_definitions(-D_WIN64)
  add_definitions(-DAMD64)
elseif (CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-D_X86_)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-D_ARM_)
  add_definitions(-DARM)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-D_ARM64_)
  add_definitions(-DARM64)
  add_definitions(-D_WIN64)
else ()
  clr_unknown_arch()
endif ()

if (WIN32)
  set(FEATURE_EVENT_TRACE 1)
endif()
if(CLR_CMAKE_PLATFORM_LINUX AND CLR_CMAKE_PLATFORM_ARCH_AMD64)
  set(FEATURE_EVENT_TRACE 1)
endif()

if(CLR_CMAKE_PLATFORM_UNIX)
  add_subdirectory(src/ToolBox/SOS/lldbplugin)
  add_subdirectory(src/pal)
  add_subdirectory(src/coreclr/hosts/unixcoreruncommon)
  add_subdirectory(src/coreclr/hosts/unixcorerun)
  add_subdirectory(src/coreclr/hosts/unixcoreconsole)
  add_subdirectory(src/ildasm/unixcoreclrloader)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CLR_CMAKE_PLATFORM_DARWIN)
  add_subdirectory(src/coreclr/hosts/osxbundlerun)
endif(CLR_CMAKE_PLATFORM_DARWIN)

# Add this subdir. We install the headers for the jit.
add_subdirectory(src/pal/prebuilt/inc)

add_subdirectory(src/debug/debug-pal)

if(WIN32)
  add_subdirectory(src/gc/sample)
endif()

# End of projects that require usage of platform include files

# Enable for UNIX altjit on Windows - set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
# Enable for UNIX altjit on Windows - add_definitions(-DCLR_CMAKE_PLATFORM_UNIX=1)

# Disable the following line for UNIX altjit on Windows
set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.

# Include directory directives

# Include the basic prebuilt headers - required for getting fileversion resource details.
include_directories("src/pal/prebuilt/inc")

if (CLR_CMAKE_PLATFORM_UNIX)
  include_directories("src/pal/inc")
  include_directories("src/pal/inc/rt")
  include_directories("src/pal/src/safecrt")
endif (CLR_CMAKE_PLATFORM_UNIX)

# Libraries

if (WIN32)

    # Define the CRT lib references that link into Desktop imports
    set(STATIC_MT_CRT_LIB  "libcmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
    set(STATIC_MT_VCRT_LIB  "libvcruntime$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
    set(STATIC_MT_CPP_LIB  "libcpmt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")

    # ARM64_TODO: Enable this for Windows Arm64
    if (NOT CLR_CMAKE_PLATFORM_ARCH_ARM64)
      # Define the uCRT lib reference
      set(STATIC_MT_UCRT_LIB  "libucrt$<$<OR:$<CONFIG:Debug>,$<CONFIG:Checked>>:d>.lib")
    endif()

endif(WIN32)

# Definition directives

if (CLR_CMAKE_PLATFORM_UNIX)

  if(CLR_CMAKE_PLATFORM_DARWIN)
    add_definitions(-D_XOPEN_SOURCE)
  endif(CLR_CMAKE_PLATFORM_DARWIN)

  if (CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
    add_definitions(-DUNIX_AMD64_ABI)
  elseif (CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM)
    add_definitions(-DUNIX_ARM_ABI)
  endif()

endif(CLR_CMAKE_PLATFORM_UNIX)

add_definitions(-D_BLD_CLR)
add_definitions(-DDEBUGGING_SUPPORTED)
add_definitions(-DPROFILING_SUPPORTED)

if(WIN32)
  add_definitions(-DWIN32)
  add_definitions(-D_WIN32)
  add_definitions(-DWINVER=0x0602)
  add_definitions(-D_WIN32_WINNT=0x0602)
  add_definitions(-DWIN32_LEAN_AND_MEAN=1)
  if(CLR_CMAKE_PLATFORM_ARCH_AMD64 OR CLR_CMAKE_PLATFORM_ARCH_I386)
    add_definitions(-D_CRT_SECURE_NO_WARNINGS)
    # Only enable edit and continue on windows x86 and x64
    # exclude Linux, arm & arm64
    add_definitions(-DEnC_SUPPORTED)
  endif(CLR_CMAKE_PLATFORM_ARCH_AMD64 OR CLR_CMAKE_PLATFORM_ARCH_I386)  
endif(WIN32)

# Features - please keep them alphabetically sorted

add_definitions(-DFEATURE_APPDOMAIN_RESOURCE_MONITORING)
if(WIN32)
    add_definitions(-DFEATURE_APPX)
endif(WIN32)
if(CLR_CMAKE_PLATFORM_ARCH_AMD64 OR CLR_CMAKE_PLATFORM_ARCH_ARM OR CLR_CMAKE_PLATFORM_ARCH_ARM64)
    add_definitions(-DFEATURE_ARRAYSTUB_AS_IL)
endif()

add_definitions(-DFEATURE_ASYNC_IO)
add_definitions(-DFEATURE_BCL_FORMATTING)
add_definitions(-DFEATURE_COLLECTIBLE_TYPES)

if(WIN32)
    add_definitions(-DFEATURE_CLASSIC_COMINTEROP)
    add_definitions(-DFEATURE_COMINTEROP)
    add_definitions(-DFEATURE_COMINTEROP_APARTMENT_SUPPORT)
    add_definitions(-DFEATURE_COMINTEROP_UNMANAGED_ACTIVATION)
    add_definitions(-DFEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION)
endif(WIN32)

add_definitions(-DFEATURE_CORECLR)
if (CLR_CMAKE_PLATFORM_UNIX)
  add_definitions(-DFEATURE_COREFX_GLOBALIZATION)
endif(CLR_CMAKE_PLATFORM_UNIX)
add_definitions(-DFEATURE_CORESYSTEM)
add_definitions(-DFEATURE_CORRUPTING_EXCEPTIONS)
if(WIN32)
    add_definitions(-DFEATURE_CRYPTO)
endif(WIN32)
if(CLR_CMAKE_PLATFORM_UNIX)
    add_definitions(-DFEATURE_DBGIPC_TRANSPORT_DI)
    add_definitions(-DFEATURE_DBGIPC_TRANSPORT_VM)
endif(CLR_CMAKE_PLATFORM_UNIX)
if(FEATURE_EVENT_TRACE)
    add_definitions(-DFEATURE_EVENT_TRACE=1)
    if(CLR_CMAKE_PLATFORM_UNIX)
        add_definitions(-DFEATURE_EVENTSOURCE_XPLAT=1)
    endif(CLR_CMAKE_PLATFORM_UNIX)
endif(FEATURE_EVENT_TRACE)
add_definitions(-DFEATURE_EXCEPTIONDISPATCHINFO)
add_definitions(-DFEATURE_FRAMEWORK_INTERNAL)
# NetBSD doesn't implement this feature
if(NOT CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
    add_definitions(-DFEATURE_HIJACK)
endif(NOT CLR_CMAKE_PLATFORM_UNIX_TARGET_ARM AND NOT CMAKE_SYSTEM_NAME STREQUAL NetBSD)
add_definitions(-DFEATURE_HOST_ASSEMBLY_RESOLVER)
add_definitions(-DFEATURE_HOSTED_BINDER)
add_definitions(-DFEATURE_ICASTABLE)
if (CLR_CMAKE_PLATFORM_UNIX OR CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-DFEATURE_IMPLICIT_TLS)
  set(FEATURE_IMPLICIT_TLS 1)
endif(CLR_CMAKE_PLATFORM_UNIX OR CLR_CMAKE_PLATFORM_ARCH_ARM64)
if(WIN32)
    add_definitions(-DFEATURE_ISOSTORE)
    add_definitions(-DFEATURE_ISOSTORE_LIGHT)
endif(WIN32)
add_definitions(-DFEATURE_ISYM_READER)
add_definitions(-DFEATURE_LEGACYNETCF)
if(WIN32)
    add_definitions(-DFEATURE_LEGACYNETCFCRYPTO)
endif(WIN32)
add_definitions(-DFEATURE_LEGACYNETCF_DBG_HOST_CONTROL)
add_definitions(-DFEATURE_LEGACYNETCFFAS)
if(WIN32)
add_definitions(-DFEATURE_LEGACYSURFACE)
endif(WIN32)
add_definitions(-DFEATURE_LOADER_OPTIMIZATION)
add_definitions(-DFEATURE_MANAGED_ETW)
add_definitions(-DFEATURE_MANAGED_ETW_CHANNELS)
add_definitions(-DFEATURE_MAIN_CLR_MODULE_USES_CORE_NAME)
add_definitions(-DFEATURE_MERGE_CULTURE_SUPPORT_AND_ENGINE)
if(WIN32)
# Disable the following for UNIX altjit on Windows
add_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)
endif(WIN32)
add_definitions(-DFEATURE_MULTICOREJIT)
add_definitions(-DFEATURE_NORM_IDNA_ONLY)
if(CLR_CMAKE_PLATFORM_UNIX)
  add_definitions(-DFEATURE_PAL)
  add_definitions(-DFEATURE_PAL_SXS)
endif(CLR_CMAKE_PLATFORM_UNIX)
if(CLR_CMAKE_PLATFORM_LINUX)
    add_definitions(-DFEATURE_PERFMAP)
endif(CLR_CMAKE_PLATFORM_LINUX)
add_definitions(-DFEATURE_PREJIT)
add_definitions(-DFEATURE_RANDOMIZED_STRING_HASHING)
if(NOT DEFINED CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-DFEATURE_READYTORUN)
  set(FEATURE_READYTORUN 1)
endif(NOT DEFINED CLR_CMAKE_PLATFORM_ARCH_ARM64)
add_definitions(-DFEATURE_STANDALONE_SN)
add_definitions(-DFEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED)
add_definitions(-DFEATURE_STRONGNAME_MIGRATION)
if(WIN32)
    add_definitions(-DFEATURE_STRONGNAME_TESTKEY_ALLOWED)
endif(WIN32)
if (CLR_CMAKE_PLATFORM_UNIX OR CLR_CMAKE_PLATFORM_ARCH_ARM64)
    add_definitions(-DFEATURE_STUBS_AS_IL)
endif(CLR_CMAKE_PLATFORM_UNIX OR CLR_CMAKE_PLATFORM_ARCH_ARM64)
add_definitions(-DFEATURE_SVR_GC)
add_definitions(-DFEATURE_SYMDIFF)
add_definitions(-DFEATURE_SYNTHETIC_CULTURES)
if(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
  add_definitions(-DFEATURE_UNIX_AMD64_STRUCT_PASSING)
  add_definitions(-DFEATURE_UNIX_AMD64_STRUCT_PASSING_ITF)
endif (CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
add_definitions(-DFEATURE_USE_ASM_GC_WRITE_BARRIERS)
add_definitions(-DFEATURE_VERSIONING)
if(WIN32)
    add_definitions(-DFEATURE_VERSIONING_LOG)
endif(WIN32)
add_definitions(-DFEATURE_WIN32_REGISTRY)
add_definitions(-DFEATURE_WINDOWSPHONE)
add_definitions(-DFEATURE_WINMD_RESILIENT)
if(WIN32)
    add_definitions(-DFEATURE_X509)
    add_definitions(-DFEATURE_X509_SECURESTRINGS)
endif(WIN32)

if(CLR_CMAKE_BUILD_TESTS)
  add_subdirectory(tests)
endif(CLR_CMAKE_BUILD_TESTS)

if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
    set(ARCH_SOURCES_DIR amd64)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM64)
    set(ARCH_SOURCES_DIR arm64)
elseif (CLR_CMAKE_PLATFORM_ARCH_ARM)
    set(ARCH_SOURCES_DIR arm)
elseif (CLR_CMAKE_PLATFORM_ARCH_I386)
    set(ARCH_SOURCES_DIR i386)
else ()
    clr_unknown_arch()
endif ()

add_definitions(-D_SECURE_SCL=0)
add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

# Compiler options

if(WIN32)
  add_compile_options(/FIWarningControl.h) # force include of WarningControl.h
  add_compile_options(/Zl) # omit default library name in .OBJ
endif(WIN32)

# Microsoft.Dotnet.BuildTools.Coreclr version
set(BuildToolsVersion "1.0.4-prerelease")
set(BuildToolsDir "${CLR_DIR}/packages/Microsoft.DotNet.BuildTools.CoreCLR/${BuildToolsVersion}")

add_subdirectory(src)
