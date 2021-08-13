cmake_minimum_required(VERSION 3.16)
cmake_policy(SET CMP0091 NEW)

# C Compiler flags
SET (CMAKE_C_FLAGS_INIT                     "/FC")
SET (CMAKE_C_FLAGS_DEBUG_INIT               "/Od /Zi")
SET (CMAKE_C_FLAGS_RELEASE_INIT             "/Ox")
SET (CMAKE_C_FLAGS_RELWITHDEBINFO_INIT      "/O2 /Zi")
SET (CMAKE_ASM_MASM_FLAGS                   "${CMAKE_ASM_MASM_FLAGS} /ZH:SHA_256")

# Configuration of our libray specs and our directories
SET (CMAKE_INCLUDE_CURRENT_DIR              ON)
SET (CMAKE_SHARED_LIBRARY_PREFIX            "")

set(__SharedLinkArgs)
set(__LinkArgs)

# Force an out of source build
if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

if(STATIC_LIBS_ONLY)
    # Suppress exporting of the PAL APIs
    add_definitions(-DPALEXPORT=EXTERN_C)

    set(GEN_SHARED_LIB 0)
    set(STATIC_LIB_DESTINATION lib)
else()
    set(GEN_SHARED_LIB 1)
    set(STATIC_LIB_DESTINATION .)
endif()

project(CoreFX)

# The following options are set by the razzle build
add_compile_options(/d2Zi+)       # make optimized builds debugging easier
add_compile_options(/nologo)      # Suppress Startup Banner
add_compile_options(/Oi)          # enable intrinsics
add_compile_options(/Oy-)         # disable suppressing of the creation of frame pointers on the call stack for quicker function calls
add_compile_options(/U_MT)        # undefine the predefined _MT macro
add_compile_options(/GF)          # enable read-only string pooling
add_compile_options(/Gm-)         # disable minimal rebuild
string(REPLACE "/EHsc" "" CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
add_compile_options(/EHa)         # enable C++ EH (w/ SEH exceptions)
add_compile_options(/Zp8)         # pack structs on 8-byte boundary
add_compile_options(/Gy)          # separate functions for linker
add_compile_options(/Zc:forScope) # C++ language conformance: enforce Standard C++ for scoping rules
string(REPLACE "/GR " " " CMAKE_CXX_FLAGS "${CMAKE_CXX_FLAGS}")
add_compile_options(/GR-)         # disable C++ RTTI
add_compile_options(/FC)          # use full pathnames in diagnostics
add_compile_options(/MP)          # Build with Multiple Processes (number of processes equal to the number of processors)
add_compile_options(/GS)          # Buffer Security Check
add_compile_options(/Zm200)       # Specify Precompiled Header Memory Allocation Limit of 150MB
add_compile_options(/Zi)          # enable debugging information
add_compile_options(/Zl)          # enable debugging information
add_compile_options(/wd4960 /wd4961 /wd4603 /wd4627 /wd4838 /wd4456 /wd4457 /wd4458 /wd4459 /wd4091 /we4640)
add_compile_options(/ZH:SHA_256) # use SHA256 for generating hashes of compiler processed source files.

if (${CLR_CMAKE_HOST_ARCH} STREQUAL "x86")
    add_compile_options(/Gz)
endif ()

# enable control-flow-guard support for native components
add_compile_options(/guard:cf)
list(APPEND __SharedLinkArgs /guard:cf)

if (${CLR_CMAKE_HOST_ARCH} STREQUAL "x86_64" OR ${CLR_CMAKE_HOST_ARCH} STREQUAL "amd64" OR ${CLR_CMAKE_HOST_ARCH} STREQUAL "x64")
    # Enable EH continuation table and CETCOMPAT for native components
    add_compile_options(/guard:ehcont)
    list(APPEND __SharedLinkArgs /guard:ehcont)
    list(APPEND __SharedLinkArgs /CETCOMPAT)
endif ()

# Statically linked CRT (libcmt[d].lib, libvcruntime[d].lib and libucrt[d].lib) by default. This is done to avoid
# linking in VCRUNTIME140.DLL for a simplified xcopy experience by reducing the dependency on VC REDIST.
#
# For Release builds, we shall dynamically link into uCRT [ucrtbase.dll] (which is pushed down as a Windows Update on downlevel OS) but
# won't do the same for debug/checked builds since ucrtbased.dll is not redistributable and Debug/Checked builds are not
# production-time scenarios.

set(CMAKE_MSVC_RUNTIME_LIBRARY "MultiThreaded$<$<CONFIG:Debug>:Debug>")

add_compile_options($<$<CONFIG:RELEASE>:/GL>)

list(APPEND __LinkLibraries $<$<CONFIG:DEBUG>:libcmtd.lib>)
list(APPEND __LinkLibraries $<$<CONFIG:RELEASE>:libcmt.lib>)

list(APPEND __LinkLibraries $<$<CONFIG:DEBUG>:libvcruntimed.lib>)
list(APPEND __LinkLibraries $<$<CONFIG:RELEASE>:libvcruntime.lib>)

# Linker flags
list(APPEND __SharedLinkArgs /INCREMENTAL:NO)
list(APPEND __SharedLinkArgs /MANIFEST:NO)            #Do not create Side-by-Side Assembly Manifest
list(APPEND __SharedLinkArgs /LARGEADDRESSAWARE)      # can handle addresses larger than 2 gigabytes
list(APPEND __SharedLinkArgs /RELEASE)                #sets the checksum in the header
list(APPEND __SharedLinkArgs /NXCOMPAT)               #Compatible with Data Execution Prevention
list(APPEND __SharedLinkArgs /DYNAMICBASE)            #Use address space layout randomization
list(APPEND __SharedLinkArgs /DEBUGTYPE:cv,fixup)     #debugging format
list(APPEND __SharedLinkArgs /PDBCOMPRESS)            #shrink pdb size
list(APPEND __SharedLinkArgs /DEBUG)
list(APPEND __SharedLinkArgs /IGNORE:4197,4013,4254,4070,4221)

# Release build specific flags
list(APPEND __LinkArgs $<$<CONFIG:RELEASE>:/LTCG>)
list(APPEND __SharedLinkArgs $<$<CONFIG:RELEASE>:/OPT:REF>)
list(APPEND __SharedLinkArgs $<$<CONFIG:RELEASE>:/OPT:ICF>)

# Force uCRT to be dynamically linked for Release build (unless env variable CLR_CMAKE_WIN32_FORCE_STATIC_LINK is set to true)
set(CLR_CMAKE_WIN32_FORCE_STATIC_LINK $ENV{CLR_CMAKE_WIN32_FORCE_STATIC_LINK})
if(NOT CLR_CMAKE_WIN32_FORCE_STATIC_LINK)
    list(APPEND __SharedLinkArgs $<$<CONFIG:RELEASE>:/NODEFAULTLIB:libucrt.lib>)
    list(APPEND __SharedLinkArgs $<$<CONFIG:RELEASE>:/DEFAULTLIB:ucrt.lib>)
endif()

# Debug build specific flags
list(INSERT __SharedLinkArgs 0 $<$<OR:$<CONFIG:DEBUG>,$<CONFIG:CHECKED>>:/NOVCFEATURE>)

if (${CLR_CMAKE_HOST_ARCH} STREQUAL "x86_64" OR ${CLR_CMAKE_HOST_ARCH} STREQUAL "amd64" OR ${CLR_CMAKE_HOST_ARCH} STREQUAL "x64")
    add_definitions(-DTARGET_64BIT=1)
endif ()

# Do not define DEBUG. zlib has asserts under DEBUG for non-catastrophic cases,
# such as on bad user-provided inputs.  We leave NDEBUG defined, however,
# as other asserts should still be included.
add_compile_definitions($<$<CONFIG:RELEASE>:NDEBUG>)

# we only need to build System.Globalization.Native when building static libs.
if(STATIC_LIBS_ONLY)
    add_subdirectory(../Unix/System.Globalization.Native System.Globalization.Native)
endif()

add_subdirectory(System.IO.Compression.Native)
