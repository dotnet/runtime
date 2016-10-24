project(clrcompression)

if("${CMAKE_SOURCE_DIR}" STREQUAL "${CMAKE_BINARY_DIR}")
    message(FATAL_ERROR "Binary directory isn't being correctly set before calling Cmake. Tree must be built in separate directory from source.")
endif()

set(CMAKE_INCLUDE_CURRENT_DIR ON)
include (GenerateExportHeader)

if($ENV{__BuildArch} STREQUAL x86 OR $ENV{__BuildArch} STREQUAL x64)
    set(NATIVECOMPRESSION_SOURCES
        clrcompression.def
        zlib-intel/adler32.c
        zlib-intel/crc_folding.c
        zlib-intel/crc32.c
        zlib-intel/deflate_quick.c
        zlib-intel/deflate.c
        zlib-intel/fill_window_sse.c
        zlib-intel/match.c
        zlib-intel/trees.c
        zlib-intel/x86.c
        zlib-intel/zutil.c
        zlib/compress.c 
        zlib/inffast.c
        zlib/inflate.c
        zlib/inftrees.c
)
elseif($ENV{__BuildArch} STREQUAL arm OR $ENV{__BuildArch} STREQUAL arm64)
    set(NATIVECOMPRESSION_SOURCES
        clrcompression.def
        zlib/adler32.c
        zlib/compress.c
        zlib/crc32.c
        zlib/deflate.c
        zlib/inffast.c
        zlib/inflate.c
        zlib/inftrees.c
        zlib/trees.c
        zlib/zutil.c
    )
endif()

if ($ENV{__VSVersion} STREQUAL vs2015)
    add_definitions(-DHAVE_VSNPRINTF)
endif ()

if(__LinkCustomCRT STREQUAL true)
    set(NATIVECOMPRESSION_SOURCES
        ${NATIVECOMPRESSION_SOURCES}
        crt.cpp
    )
endif()

# Include 'bin/obj' dir since it contains _version.h
include_directories("${CMAKE_BINARY_DIR}/../../")

add_library(clrcompression
    SHARED
    ${NATIVECOMPRESSION_SOURCES}
    # This will add versioning to the library
    ${CMAKE_SOURCE_DIR}/../../../Tools/NativeVersion.rc
)

# Allow specification of arguments that should be passed to the linker
SET_TARGET_PROPERTIES(clrcompression PROPERTIES LINK_FLAGS ${__LinkArgs})

# Allow specification of libraries that should be linked against
# CMake doesn't like space delimiters as input to target_link_libraries
separate_arguments(linker_libs_sanitized WINDOWS_COMMAND ${__LinkLibraries})
target_link_libraries(clrcompression ${linker_libs_sanitized})

GENERATE_EXPORT_HEADER( clrcompression
     BASE_NAME clrcompression
     EXPORT_MACRO_NAME clrcompression_EXPORT
     EXPORT_FILE_NAME clrcompression_Export.h
     STATIC_DEFINE clrcompression_BUILT_AS_STATIC
)

install (TARGETS clrcompression DESTINATION .)
install (FILES ${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_BUILD_TYPE}/clrcompression.pdb DESTINATION .)

