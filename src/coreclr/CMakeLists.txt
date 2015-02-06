# Require at least version 2.8.12 of CMake
cmake_minimum_required(VERSION 2.8.12)

# Set the project name
project(CoreCLR)

if(CMAKE_SYSTEM_NAME STREQUAL Linux)
    set(CLR_CMAKE_PLATFORM_UNIX 1)
    set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
endif(CMAKE_SYSTEM_NAME STREQUAL Linux)

# Build a list of compiler definitions by putting -D in front of each define.
function(get_compile_definitions DefinitionName)
    # Get the current list of definitions
    get_directory_property(COMPILE_DEFINITIONS_LIST COMPILE_DEFINITIONS)

    foreach(DEFINITION ${COMPILE_DEFINITIONS_LIST})
        if (${DEFINITION} MATCHES "^\\$<\\$<CONFIG:([^>]+)>:([^>]+)>$")
            # The entries that contain generator expressions must have the -D inside of the 
            # expression. So we transform e.g. $<$<CONFIG:Debug>:_DEBUG> to $<$<CONFIG:Debug>:-D_DEBUG>
            set(DEFINITION "$<$<CONFIG:${CMAKE_MATCH_1}>:-D${CMAKE_MATCH_2}>")
        else()
            set(DEFINITION -D${DEFINITION})
        endif()
        set(DEFINITIONS ${DEFINITIONS} ${DEFINITION})
    endforeach()
    set(${DefinitionName} ${DEFINITIONS} PARENT_SCOPE)
endfunction(get_compile_definitions DefinitionName)

# Build a list of include directories by putting -I in front of each include dir.
function(get_include_directories IncludeDirectories)
    get_directory_property(dirs INCLUDE_DIRECTORIES)
    foreach(dir ${dirs})
        set(INC_DIRECTORIES ${INC_DIRECTORIES} -I${dir})
    endforeach()
    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories IncludeDirectories)

# Set the passed in RetSources variable to the list of sources with added current source directory
# to form absolute paths. 
# The parameters after the RetSources are the input files. 
function(convert_to_absolute_path RetSources)
    set(Sources ${ARGN})
    foreach(Source ${Sources})
        set(AbsolutePathSources ${AbsolutePathSources} ${CMAKE_CURRENT_SOURCE_DIR}/${Source})
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


# Includes

if (WIN32)
  # For multi-configuration toolset (as Visual Studio)
  # set the different configuration defines.
  # First DEBUG
  foreach (Definition ${CLR_DEFINES_DEBUG_INIT})
    set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS $<$<CONFIG:DEBUG>:${Definition}>)
  endforeach (Definition ${CLR_DEFINES_DEBUG_INIT})

  # Then RELEASE
  foreach (Definition ${CLR_DEFINES_RELEASE_INIT})
    set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS $<$<CONFIG:RELEASE>:${Definition}>)
  endforeach (Definition ${CLR_DEFINES_RELEASE_INIT})

  # And then RELWITHDEBINFO
  foreach (Definition ${CLR_DEFINES_RELWITHDEBINFO_INIT})
    set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS $<$<CONFIG:RELWITHDEBINFO>:${Definition}>)
  endforeach (Definition ${CLR_DEFINES_RELWITHDEBINFO_INIT})

elseif (CLR_CMAKE_PLATFORM_UNIX)
  # Set the values to display when interactively configuring CMAKE_BUILD_TYPE
  set_property(CACHE CMAKE_BUILD_TYPE PROPERTY STRINGS "DEBUG;RELEASE;RELWITHDEBINFO")

  # Use uppercase CMAKE_BUILD_TYPE for the string comparisons below
  string(TOUPPER ${CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)

  # For single-configuration toolset
  # set the different configuration defines.
  if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG)
    # First DEBUG
    set_property(DIRECTORY  PROPERTY COMPILE_DEFINITIONS ${CLR_DEFINES_DEBUG_INIT})
  elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
    # Then RELEASE
    set_property(DIRECTORY PROPERTY COMPILE_DEFINITIONS ${CLR_DEFINES_RELEASE_INIT})
  elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELWITHDEBINFO)
    # And then RELWITHDEBINFO
    set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS ${CLR_DEFINES_RELWITHDEBINFO_INIT})
  else ()
    message(FATAL_ERROR "Unknown build type! Set CMAKE_BUILD_TYPE to DEBUG, RELEASE, or RELWITHDEBINFO!")
  endif ()

endif(WIN32)

if(CLR_CMAKE_PLATFORM_UNIX)
    add_subdirectory(src/pal)
    add_subdirectory(src/coreclr/hosts/unixcorerun)
endif(CLR_CMAKE_PLATFORM_UNIX)

# Set to 1 if you want to clear the CMAKE initial compiler flags and set all the flags explicitly
# or to 0 if the CMake generated flags should be used

# Enable for UNIX altjit on Windows - set(CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64 1)
# Enable for UNIX altjit on Windows - add_definitions(-DCLR_CMAKE_PLATFORM_UNIX=1)

if (WIN32)
  set(OVERRIDE_CMAKE_CXX_FLAGS 1)

  # Set flag to indicate if this will be a 64bit Windows build
  if(CMAKE_SYSTEM_PROCESSOR STREQUAL AMD64)
    set(IS_64BIT_BUILD 1)
  endif (CMAKE_SYSTEM_PROCESSOR STREQUAL AMD64)
  
elseif (CLR_CMAKE_PLATFORM_UNIX)  
  # Set flag to indicate if this will be a 64bit Linux build
  if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64)
    set(IS_64BIT_BUILD 1)  
  endif (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64)
  
  add_definitions(-DFEATURE_IMPLICIT_TLS)
endif(WIN32)

if (OVERRIDE_CMAKE_CXX_FLAGS)

# Compile options for targeting windows
if (WIN32)

# The following options are set by the razzle build
add_compile_options(/TP) # compile all files as C++
add_compile_options(/FIWarningControl.h) # force include of WarningControl.h
add_compile_options(/Zi) # enable debugging information
add_compile_options(/d2Zi+) # make optimized builds debugging easier
add_compile_options(/nologo) # Suppress Startup Banner
add_compile_options(/W3) # set warning level to 3
add_compile_options(/WX) # treat warnings as errors
add_compile_options(/O1) # minimize space
add_compile_options(/Oi) # enable intrinsics
add_compile_options(/Oy-) # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
add_compile_options(/U_MT) # undefine the predefined _MT macro
add_compile_options(/GF) # enable read-only string pooling
add_compile_options(/Gm-) # disable minimal rebuild
add_compile_options(/EHa) # enable C++ EH (w/ SEH exceptions)
add_compile_options(/Zp8) # pack structs on 8-byte boundary
add_compile_options(/GS) # enable security checks
add_compile_options(/Gy) # separate functions for linker
add_compile_options(/Zc:wchar_t-) # C++ language conformance: wchar_t is NOT the native type, but a typedef
add_compile_options(/Zc:forScope) # C++ language conformance: enforce Standard C++ for scoping rules
add_compile_options(/GR-) # disable C++ RTTI
add_compile_options(/FC) # use full pathnames in diagnostics
add_compile_options(/Zl) # omit default library name in .OBJ
add_compile_options(/MP) # Build with Multiple Processes (number of processes equal to the number of processors)
add_compile_options(/GS) # Buffer Security Check
add_compile_options(/Zm200) # Specify Precompiled Header Memory Allocation Limit of 150MB
add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)

if (IS_64BIT_BUILD EQUAL 1)
# The generator expression in the following command means that the /homeparams option is added only for debug builds
add_compile_options($<$<CONFIG:Debug>:/homeparams>) # Force parameters passed in registers to be written to the stack
endif (IS_64BIT_BUILD EQUAL 1)

# Disable the following line for UNIX altjit on Windows
set(CMAKE_CXX_STANDARD_LIBRARIES "") # do not link against standard win32 libs i.e. kernel32, uuid, user32, etc.

# Linker flags
#
# Disable the following line for UNIX altjit on Windows
set(CMAKE_SHARED_LINKER_FLAGS "/NODEFAULTLIB") #do not use default libraries when resolving external references
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /MANIFEST:NO") #Do not create Side-by-Side Assembly Manifest
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SUBSYSTEM:WINDOWS,6.00") #windows subsystem
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /LARGEADDRESSAWARE") # can handle addresses larger than 2 gigabytes
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /RELEASE") #sets the checksum in the header
if (IS_64BIT_BUILD EQUAL 0)
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /SAFESEH:NO") #Image does not have Safe Exception Handlers..valid only for x86
endif (IS_64BIT_BUILD EQUAL 0)
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /NXCOMPAT") #Compatible with Data Execution Prevention
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DYNAMICBASE") #Use address space layout randomization
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUGTYPE:cv,fixup") #debugging format
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /PDBCOMPRESS") #shrink pdb size
set(CMAKE_SHARED_LINKER_FLAGS "${CMAKE_SHARED_LINKER_FLAGS} /DEBUG /INCREMENTAL:NO /OPT:REF /OPT:ICF /IGNORE:4197,4013,4254,4070,4221")

set(CMAKE_EXE_LINKER_FLAGS "${CMAKE_EXE_LINKER_FLAGS} /DEBUG /PDBCOMPRESS")

# Debug build specific flags
set(CMAKE_SHARED_LINKER_FLAGS_DEBUG "/NOVCFEATURE")

# Release build specific flags
set(CMAKE_SHARED_LINKER_FLAGS_RELEASE "${CMAKE_SHARED_LINKER_FLAGS_RELEASE} /LTCG")

# ReleaseWithDebugInfo build specific flags
set(CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO "${CMAKE_SHARED_LINKER_FLAGS_RELWITHDEBINFO} /LTCG")
endif (WIN32)

endif (OVERRIDE_CMAKE_CXX_FLAGS)

if(CLR_CMAKE_PLATFORM_UNIX)
add_definitions(-DDISABLE_CONTRACTS)
# The -ferror-limit is helpful during the porting, it makes sure the compiler doesn't stop
# after hitting just about 20 errors.
add_compile_options(-ferror-limit=4096)

# Disabled warnings
add_compile_options(-Wno-reorder)
add_compile_options(-Wno-ignored-attributes)
add_compile_options(-Wno-unknown-pragmas)
add_compile_options(-Wno-unused-private-field)
add_compile_options(-Wno-new-returns-null)
add_compile_options(-Wno-dangling-else)
add_compile_options(-Wno-implicit-exception-spec-mismatch)
add_compile_options(-Wno-deprecated-register)
add_compile_options(-Wno-parentheses)
add_compile_options(-Wno-overloaded-virtual)
add_compile_options(-Wno-unused-variable)
add_compile_options(-Wno-missing-declarations)
add_compile_options(-Wno-switch)
add_compile_options(-Wno-extern-initializer)
add_compile_options(-Wno-microsoft)
add_compile_options(-Wno-mismatched-tags)
add_compile_options(-Wno-ignored-qualifiers)
add_compile_options(-Wno-tautological-constant-out-of-range-compare)
add_compile_options(-Wno-c++11-compat-deprecated-writable-strings)
add_compile_options(-Wno-unneeded-internal-declaration)
add_compile_options(-Wno-tautological-compare)
add_compile_options(-Wno-constant-logical-operand)
add_compile_options(-Wno-unused-function)
add_compile_options(-Wno-extra-tokens)
add_compile_options(-Wno-self-assign)
add_compile_options(-Wno-bitfield-constant-conversion)
add_compile_options(-Wno-unused-value)

add_compile_options(-Wno-unknown-warning-option)

#These seem to indicate real issues
add_compile_options(-Wno-invalid-offsetof)
add_compile_options(-Wno-return-type)
add_compile_options(-Wno-dynamic-class-memaccess)
add_compile_options(-Wno-int-to-pointer-cast)
add_compile_options(-Wno-delete-non-virtual-dtor)
add_compile_options(-Wno-enum-compare)

# The -fms-extensions enable the stuff like __if_exists, __declspec(uuid()), etc.
add_compile_options(-fms-extensions )
#-fms-compatibility      Enable full Microsoft Visual C++ compatibility
#-fms-extensions         Accept some non-standard constructs supported by the Microsoft compiler
# The newer version of clang hits this seemly benign error that this disables.
add_compile_options(-Wno-incompatible-ms-struct)
endif(CLR_CMAKE_PLATFORM_UNIX)

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
    set(STATIC_MT_CRT_LIB  "libcmt$<$<CONFIG:Debug>:d>.lib")
    set(STATIC_MT_CPP_LIB  "libcpmt$<$<CONFIG:Debug>:d>.lib")
endif(WIN32)
# Definition directives

# The following defines were extracted from the official amd64 debug / release builds.
if (WIN32)
  add_definitions(-DFEATURE_EVENT_TRACE=1)
endif (WIN32)

if (CLR_CMAKE_PLATFORM_UNIX)
  add_definitions(-DPLATFORM_UNIX=1)
  add_definitions(-D__LINUX__=1)
  add_definitions(-DFEATURE_PAL_SXS)

  if(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64)
    message("Detected Linux x86_64")
    add_definitions(-DLINUX64)
    add_definitions(-DBIT64=1)
    add_definitions(-DFEATURE_PAL)
  else (CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64)
    message(FATAL_ERROR "error: Detected non x86_64 target processor. Not supported!")
  endif(CMAKE_SYSTEM_PROCESSOR STREQUAL x86_64)
endif(CLR_CMAKE_PLATFORM_UNIX)


add_definitions(-D_CRT_STDIO_ARBITRARY_WIDE_SPECIFIERS)
add_definitions(-DDEV10)
add_definitions(-DWIN32)
add_definitions(-DDEVL=1)
add_definitions(-D_WIN32_WINNT=0x0602)
add_definitions(-D_WIN32_IE=0x0900)
add_definitions(-DWINNT=1)
add_definitions(-DNT_INST=0)
add_definitions(-DCONDITION_HANDLING=1)
add_definitions(-DNTDDI_VERSION=NTDDI_WIN8)

if (IS_64BIT_BUILD EQUAL 1)
  if (CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
    add_definitions(-DDBG_TARGET_AMD64_UNIX)
  endif (CLR_CMAKE_PLATFORM_UNIX_TARGET_AMD64)
  add_definitions(-D_TARGET_AMD64_=1)
  add_definitions(-DDBG_TARGET_AMD64)
else (IS_64BIT_BUILD EQUAL 1)
  # TODO: Support this
  message(FATAL_ERROR "Not Implemented!")
endif (IS_64BIT_BUILD EQUAL 1)

if(WIN32)
add_definitions(-D_CRT_SECURE_NO_WARNINGS)
endif(WIN32)
add_definitions(-DNTMAKEENV)
add_definitions(-D_BLD_CLR)
add_definitions(-DWINVER=0x0602)
add_definitions(-DWIN32_LEAN_AND_MEAN=1)
add_definitions(-DDEBUGGING_SUPPORTED)
if(WIN32)
    # Disable edit and continue on Linux
    add_definitions(-DEnC_SUPPORTED)
endif(WIN32)
add_definitions(-DFEATURE_APPDOMAIN_RESOURCE_MONITORING)
add_definitions(-DFEATURE_ARRAYSTUB_AS_IL)
if (CLR_CMAKE_PLATFORM_UNIX)
    add_definitions(-DFEATURE_STUBS_AS_IL)
    add_definitions(-DUNIX_AMD64_ABI)
endif(CLR_CMAKE_PLATFORM_UNIX)
add_definitions(-DFEATURE_ASYNC_IO)
add_definitions(-DFEATURE_BCL_FORMATTING)
if(WIN32)
    add_definitions(-DFEATURE_CLASSIC_COMINTEROP)
endif(WIN32)
add_definitions(-DFEATURE_COLLECTIBLE_TYPES)
if(WIN32)
    add_definitions(-DFEATURE_APPX)
    add_definitions(-DFEATURE_COMINTEROP)
    add_definitions(-DFEATURE_COMINTEROP_APARTMENT_SUPPORT)
    add_definitions(-DFEATURE_COMINTEROP_UNMANAGED_ACTIVATION)
    add_definitions(-DFEATURE_COMINTEROP_WINRT_MANAGED_ACTIVATION)
endif(WIN32)
add_definitions(-DFEATURE_CORECLR)
add_definitions(-DFEATURE_CORESYSTEM)
add_definitions(-DFEATURE_CORRUPTING_EXCEPTIONS)
if(WIN32)
    add_definitions(-DFEATURE_CRYPTO)
endif(WIN32)
add_definitions(-DFEATURE_EXCEPTIONDISPATCHINFO)
add_definitions(-DFEATURE_FRAMEWORK_INTERNAL)
if(WIN32)
    add_definitions(-DFEATURE_HIJACK)
endif(WIN32)
add_definitions(-DFEATURE_HOST_ASSEMBLY_RESOLVER)
add_definitions(-DFEATURE_HOSTED_BINDER)
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
# Disable the following or UNIX altjit on Windows
add_definitions(-DFEATURE_MERGE_JIT_AND_ENGINE)
endif(WIN32)
add_definitions(-DFEATURE_NORM_IDNA_ONLY)
add_definitions(-DFEATURE_PREJIT)
if(WIN32)
    add_definitions(-DFEATURE_RANDOMIZED_STRING_HASHING)
endif(WIN32)
add_definitions(-DFEATURE_STANDALONE_SN)
add_definitions(-DFEATURE_STRONGNAME_DELAY_SIGNING_ALLOWED)
add_definitions(-DFEATURE_STRONGNAME_MIGRATION)
if(WIN32)
    add_definitions(-DFEATURE_STRONGNAME_TESTKEY_ALLOWED)
endif(WIN32)
add_definitions(-DFEATURE_SVR_GC)
add_definitions(-DFEATURE_SYNTHETIC_CULTURES)
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
    add_definitions(-DPROFILING_SUPPORTED)
endif(WIN32)
add_definitions(-DFEATURE_MULTICOREJIT)
add_definitions(-DFEATURE_USE_ASM_GC_WRITE_BARRIERS)
add_definitions(-DFEATURE_SYMDIFF)

if (IS_64BIT_BUILD EQUAL 1)
add_definitions(-D_AMD64_)
add_definitions(-D_AMD64_SIMULATOR_)
add_definitions(-D_AMD64_SIMULATOR_PERF_)
add_definitions(-D_AMD64_WORKAROUND_)
add_definitions(-D_WIN64)
add_definitions(-DAMD64)
else (IS_64BIT_BUILD EQUAL 1)
# TODO - Support this
endif (IS_64BIT_BUILD EQUAL 1)

add_definitions(-D_SKIP_IF_SIMULATOR_)
add_definitions(-D_SECURE_SCL=0)
add_definitions(-D_NEW_SDK=1)
add_definitions(-DOFFICIAL_BUILD=0)
add_definitions(-DBETA=0)
add_definitions(-DFX_BRANCH_SYNC_COUNTER_VALUE=0)
add_definitions(-DUNICODE)
add_definitions(-D_UNICODE)

if (IS_64BIT_BUILD EQUAL 1)
  set(ARCH_SOURCES_DIR amd64)
else (IS_64BIT_BUILD EQUAL 1)
  set(ARCH_SOURCES_DIR i386)
endif (IS_64BIT_BUILD EQUAL 1)

set(CLR_DIR ${CMAKE_CURRENT_SOURCE_DIR})
set(VM_DIR ${CMAKE_CURRENT_SOURCE_DIR}/src/vm)

add_subdirectory(src)
