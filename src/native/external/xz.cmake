# IMPORTANT: do not use add_compile_options(), add_definitions() or similar functions here since it will leak to the including projects

include(FetchContent)

FetchContent_Declare(
    xz
    SOURCE_DIR ${CMAKE_CURRENT_LIST_DIR}/xz
    EXCLUDE_FROM_ALL
)

# turn off multithreading support to lower the binary size
set(XZ_THREADS no)

set(XZ_LZIP_DECODER OFF)
set(XZ_LZIP_ENCODER OFF)

# turn off parts we don't need
set(BUILD_TESTING OFF)
set(XZ_DOXYGEN OFF)
set(XZ_DOC OFF)
set(XZ_NLS OFF)
set(XZ_MICROLZMA_ENCODER OFF)
set(XZ_MICROLZMA_DECODER OFF)
set(XZ_TOOL_XZ OFF)
set(XZ_TOOL_SCRIPTS OFF)
set(XZ_TOOL_XZDEC OFF)
set(XZ_TOOL_LZMADEC OFF)
set(XZ_TOOL_LZMAINFO OFF)
set(XZ_TOOL_SYMLINKS OFF)
set(XZ_TOOL_SYMLINKS_LZMA OFF)

# trick xz's cmake to avoid compiling libgnu, which we exclude from vendored sources
# since it is only used for cli tools and not for liblzma
set(HAVE_GETOPT_LONG ON)

set(__CURRENT_BUILD_SHARED_LIBS ${BUILD_SHARED_LIBS})
set(BUILD_SHARED_LIBS OFF)
FetchContent_MakeAvailable(xz)
set(BUILD_SHARED_LIBS ${__CURRENT_BUILD_SHARED_LIBS})

set(LZMA_INCLUDE_DIRS ${CMAKE_CURRENT_LIST_DIR}/xz/src/liblzma/api)

# API functions are marked for export only when HAVE_VISIBILITY=1. However,
# that happens only if BUILD_SHARED_LIBS is ON. Since we want to export the API
# functions even for static library to be able to invoke them in the resulting shared
# libraries, so we need to force HAVE_VISIBILITY=1 for static liblzma as well.
if (NOT MSVC)
    get_target_property(defs liblzma COMPILE_DEFINITIONS)
    if(defs AND NOT defs STREQUAL "defs-NOTFOUND")
        list(FILTER defs EXCLUDE REGEX "HAVE_VISIBILITY=.")
        set_property(TARGET liblzma PROPERTY COMPILE_DEFINITIONS ${defs})
    endif()
    target_compile_definitions(liblzma PRIVATE HAVE_VISIBILITY=1)
endif()

