set(CMAKE_INCLUDE_CURRENT_DIR ON)

set( GCINFO_SOURCES
  arraylist.cpp
  gcinfoencoder.cpp
)


if(CLR_CMAKE_TARGET_ARCH_I386)
  list(APPEND GCINFO_SOURCES
    ../gcdump/gcdump.cpp
    ../gcdump/${ARCH_SOURCES_DIR}/gcdumpx86.cpp
  )
endif(CLR_CMAKE_TARGET_ARCH_I386)


convert_to_absolute_path(GCINFO_SOURCES ${GCINFO_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(lib)
add_subdirectory(crossgen)

if (CLR_CMAKE_PLATFORM_ARCH_I386)
    # On x86, build RyuJIT/ARM32 cross-compiling altjit.
    add_subdirectory(gcinfo_arm)
endif ()

if (CLR_CMAKE_PLATFORM_ARCH_I386 AND WIN32)
    # On x86, build Linux x86 cross-compiling altjit.
    add_subdirectory(gcinfo_linuxx86)
endif ()

if (CLR_CMAKE_PLATFORM_ARCH_AMD64)
    # On amd64, build RyuJIT/ARM64 cross-compiling altjit.
    add_subdirectory(gcinfo_arm64)
endif ()

_install (FILES gcinfoencoder.cpp
               DESTINATION gcinfo)
