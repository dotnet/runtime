
include(${CLR_DIR}/dac.cmake)

add_definitions(-DFEATURE_NO_HOST)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_definitions(-D_TARGET_AMD64_=1)
  add_definitions(-DDBG_TARGET_64BIT=1)
  add_definitions(-DDBG_TARGET_AMD64=1)
  add_definitions(-DDBG_TARGET_WIN64=1)
  add_definitions(-D_WIN64=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_I386)
  add_definitions(-D_TARGET_X86_=1)
  add_definitions(-DDBG_TARGET_X86=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_definitions(-D_TARGET_ARM_=1)
  add_definitions(-DDBG_TARGET_32BIT=1)
  add_definitions(-DDBG_TARGET_ARM=1)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_definitions(-D_TARGET_ARM64_=1)
  add_definitions(-DDBG_TARGET_64BIT=1)
  add_definitions(-DDBG_TARGET_ARM64=1)
  add_definitions(-DDBG_TARGET_WIN64=1)
  add_definitions(-D_WIN64=1)
else()
  clr_unknown_arch()
endif()

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

include_directories(${ARCH_SOURCES_DIR})
  list(APPEND DACCESS_SOURCES
    ${ARCH_SOURCES_DIR}/primitives.cpp
  )

convert_to_absolute_path(DACCESS_SOURCES ${DACCESS_SOURCES})

add_precompiled_header(stdafx.h stdafx.cpp DACCESS_SOURCES)

add_library_clr(daccess ${DACCESS_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_dependencies(daccess coreclr)
endif(CLR_CMAKE_PLATFORM_UNIX)
