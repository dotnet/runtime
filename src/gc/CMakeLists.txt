project(clrgc)

set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(env)

set(SOURCES
    gccommon.cpp
    gceewks.cpp
    gcscan.cpp
    gcwks.cpp
    handletable.cpp
    handletablecache.cpp
    handletablecore.cpp
    handletablescan.cpp
    objecthandle.cpp
)

if(WIN32)
    list(APPEND SOURCES
        env/gcenv.windows.cpp)
else()
    list(APPEND SOURCES
        env/gcenv.unix.cpp)
endif()

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-D_TARGET_AMD64_=1)
  add_definitions(-D_WIN64=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-D_TARGET_X86_=1)
  add_definitions(-D_WIN32=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-D_TARGET_ARM_=1)
  add_definitions(-D_WIN32=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-D_TARGET_ARM64_=1)
  add_definitions(-D_WIN64=1)
else()
  clr_unknown_arch()
endif()

add_compile_options(-Wno-format)
add_compile_options(-Wno-unused-variable)
add_compile_options(-Wno-unused-private-field)
add_compile_options(-Wno-tautological-undefined-compare)

add_library(clrgc STATIC ${SOURCES})

