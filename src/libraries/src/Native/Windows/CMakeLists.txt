cmake_minimum_required(VERSION 2.8.12)

# C Compiler flags
SET (CMAKE_C_FLAGS_INIT                     "/W0 /FC")
SET (CMAKE_C_FLAGS_DEBUG_INIT               "/Od /Zi")
SET (CMAKE_C_FLAGS_RELEASE_INIT             "/Ox")
SET (CMAKE_C_FLAGS_RELWITHDEBINFO_INIT      "/O2 /Zi")
SET (CMAKE_ASM_MASM_FLAGS                   "${CMAKE_ASM_MASM_FLAGS} /ZH:SHA_256")

# Configuration of our libray specs and our directories
SET (CMAKE_INSTALL_PREFIX                   $ENV{__CMakeBinDir})
SET (CMAKE_INCLUDE_CURRENT_DIR              ON)
SET (CMAKE_SHARED_LIBRARY_PREFIX            "")
SET (__LinkArgs $ENV{__LinkArgs})

# Force an out of source build
if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

project(CoreFX)
string(TOUPPER $ENV{CMAKE_BUILD_TYPE} UPPERCASE_CMAKE_BUILD_TYPE)

# The following options are set by the razzle build
add_compile_options(/d2Zi+)       # make optimized builds debugging easier
add_compile_options(/nologo)      # Suppress Startup Banner
add_compile_options(/Oi)          # enable intrinsics
add_compile_options(/Oy-)         # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
add_compile_options(/U_MT)        # undefine the predefined _MT macro
add_compile_options(/GF)          # enable read-only string pooling
add_compile_options(/Gm-)         # disable minimal rebuild
add_compile_options(/EHa)         # enable C++ EH (w/ SEH exceptions)
add_compile_options(/Zp8)         # pack structs on 8-byte boundary
add_compile_options(/Gy)          # separate functions for linker
add_compile_options(/Zc:wchar_t-) # C++ language conformance: wchar_t is NOT the native type, but a typedef
add_compile_options(/Zc:forScope) # C++ language conformance: enforce Standard C++ for scoping rules
add_compile_options(/GR-)         # disable C++ RTTI
add_compile_options(/FC)          # use full pathnames in diagnostics
add_compile_options(/MP)          # Build with Multiple Processes (number of processes equal to the number of processors)
add_compile_options(/GS)          # Buffer Security Check
add_compile_options(/Zm200)       # Specify Precompiled Header Memory Allocation Limit of 150MB
add_compile_options(/Zi)          # enable debugging information
add_compile_options(/Zl)          # enable debugging information
add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)
add_compile_options(/ZH:SHA_256) # use SHA256 for generating hashes of compiler processed source files.

if ($ENV{__BuildArch} STREQUAL "x86")
    add_compile_options(/Gz)
endif ()

if (($ENV{__BuildArch} STREQUAL "arm") OR ($ENV{__BuildArch} STREQUAL "arm64"))
    if(NOT DEFINED CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION OR CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION STREQUAL "" )
          message(FATAL_ERROR "Windows SDK is required for the ARM32 or ARM64 build.")
      else()
          message("Using Windows SDK version ${CMAKE_VS_WINDOWS_TARGET_PLATFORM_VERSION}")
      endif()
endif ()

# enable control-flow-guard support for native components
add_compile_options(/guard:cf) 
set(__LinkArgs "${__LinkArgs} /GUARD:CF")

# Statically linked CRT (libcmt[d].lib, libvcruntime[d].lib and libucrt[d].lib) by default. This is done to avoid
# linking in VCRUNTIME140.DLL for a simplified xcopy experience by reducing the dependency on VC REDIST.
#
# For Release builds, we shall dynamically link into uCRT [ucrtbase.dll] (which is pushed down as a Windows Update on downlevel OS) but
# won't do the same for debug/checked builds since ucrtbased.dll is not redistributable and Debug/Checked builds are not
# production-time scenarios.
if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL "RELEASE")
    add_compile_options(/MT)
    add_compile_options(/GL)
    add_compile_options(/O1)
    set(__LinkLibraries "${__LinkLibraries} libcmt.lib")
    set(__LinkLibraries "${__LinkLibraries} libvcruntime.lib")
    set(STATIC_UCRT_LIB "libucrt.lib")
    set(DYNAMIC_UCRT_LIB "ucrt.lib")
else()
    add_compile_options(/MTd)
    set(__LinkLibraries "${__LinkLibraries} libcmtd.lib")
    set(__LinkLibraries "${__LinkLibraries} libvcruntimed.lib")
    set(STATIC_UCRT_LIB "libucrtd.lib")
    set(DYNAMIC_UCRT_LIB "ucrtd.lib")
endif()

# Linker flags
set(__LinkArgs "${__LinkArgs} /INCREMENTAL:NO")
set(__LinkArgs "${__LinkArgs} /MANIFEST:NO")            #Do not create Side-by-Side Assembly Manifest
set(__LinkArgs "${__LinkArgs} /LARGEADDRESSAWARE")      # can handle addresses larger than 2 gigabytes
set(__LinkArgs "${__LinkArgs} /RELEASE")                #sets the checksum in the header
set(__LinkArgs "${__LinkArgs} /NXCOMPAT")               #Compatible with Data Execution Prevention
set(__LinkArgs "${__LinkArgs} /DYNAMICBASE")            #Use address space layout randomization
set(__LinkArgs "${__LinkArgs} /DEBUGTYPE:cv,fixup")     #debugging format
set(__LinkArgs "${__LinkArgs} /PDBCOMPRESS")            #shrink pdb size
set(__LinkArgs "${__LinkArgs} /DEBUG")
set(__LinkArgs "${__LinkArgs} /IGNORE:4197,4013,4254,4070,4221")

if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL "RELEASE")
    # Release build specific flags
    set(__LinkArgs "${__LinkArgs} /LTCG /OPT:REF /OPT:ICF /INCREMENTAL:NO")
    
    # Force uCRT to be dynamically linked for Release build (unless env variable CLR_CMAKE_WIN32_FORCE_STATIC_LINK is set to true)
    set(CLR_CMAKE_WIN32_FORCE_STATIC_LINK $ENV{CLR_CMAKE_WIN32_FORCE_STATIC_LINK})
    if(NOT CLR_CMAKE_WIN32_FORCE_STATIC_LINK)
      set(__LinkArgs "${__LinkArgs} /NODEFAULTLIB:libucrt.lib /DEFAULTLIB:ucrt.lib")
    endif()
    
    if ($ENV{__BuildArch} STREQUAL x86)
        set(__LinkArgs "${__LinkArgs} /SAFESEH")
    endif()
else()
    # Debug build specific flags
    set(__LinkArgs "/NOVCFEATURE ${__LinkArgs}")
endif()

if ($ENV{__BuildArch} STREQUAL "x86_64" OR $ENV{__BuildArch} STREQUAL "amd64")
    add_definitions(-DBIT64=1)
endif ()

if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL "DEBUG")
    # Do not define DEBUG. zlib has asserts under DEBUG for non-catastrophic cases,
    # such as on bad user-provided inputs.  We leave NDEBUG defined, however,
    # as other asserts should still be included.
elseif (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL "RELEASE")
    add_definitions(-DNDEBUG)
else ()
    message(FATAL_ERROR "Unknown build type. Set CMAKE_BUILD_TYPE to DEBUG or RELEASE.")
endif ()

add_subdirectory(clrcompression)
