
include(${CLR_DIR}/dac.cmake)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-D_TARGET_AMD64_=1)
add_definitions(-DDBG_TARGET_64BIT=1)
add_definitions(-DDBG_TARGET_AMD64=1)
add_definitions(-DDBG_TARGET_WIN64=1)
add_definitions(-D_WIN64=1)

include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${CLR_DIR}/src/debug/ee)
include_directories(${CLR_DIR}/src/gc)
include_directories(${CLR_DIR}/src/gcdump)

if(CLR_CMAKE_PLATFORM_UNIX)
    include_directories(${GENERATED_INCLUDE_DIR})
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

set(DACCESS_SOURCES
  dacdbiimpl.cpp
  dacdbiimpllocks.cpp
  dacdbiimplstackwalk.cpp
  daccess.cpp
  dacfn.cpp
  enummem.cpp
  fntableaccess.cpp
  inspect.cpp
  reimpl.cpp
  request.cpp
  request_svr.cpp
  stack.cpp
  task.cpp
  nidump.cpp
  datatargetadapter.cpp
)

if(IS_64BIT_BUILD EQUAL 1)
  include_directories(amd64)

  list(APPEND DACCESS_SOURCES
    amd64/primitives.cpp
  )
else()
  list(APPEND DACCESS_SOURCES
    i386/primitives.cpp
  )
endif(IS_64BIT_BUILD EQUAL 1)

convert_to_absolute_path(DACCESS_SOURCES ${DACCESS_SOURCES})

add_precompiled_header(stdafx.h stdafx.cpp DACCESS_SOURCES)

add_library(daccess ${DACCESS_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_dependencies(daccess coreclr)
endif(CLR_CMAKE_PLATFORM_UNIX)
