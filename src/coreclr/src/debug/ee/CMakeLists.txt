set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DFEATURE_NO_HOST)
add_definitions(-D_TARGET_AMD64_=1)
add_definitions(-DDBG_TARGET_64BIT=1)
add_definitions(-DDBG_TARGET_AMD64=1)
add_definitions(-DDBG_TARGET_WIN64=1)

include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})

if(CLR_CMAKE_PLATFORM_UNIX)
  add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

set(CORDBEE_SOURCES_DAC_AND_WKS
  controller.cpp
  debugger.cpp
  debuggermodule.cpp
  functioninfo.cpp
)

set(CORDBEE_SOURCES_WKS
  ${CORDBEE_SOURCES_DAC_AND_WKS}
  funceval.cpp
  rcthread.cpp
  canary.cpp
  shared.cpp
  frameinfo.cpp
  ${ARCH_SOURCES_DIR}/primitives.cpp
  ${ARCH_SOURCES_DIR}/debuggerregdisplayhelper.cpp
)

set(CORDBEE_SOURCES_DAC 
  ${CORDBEE_SOURCES_DAC_AND_WKS}
)

if(CLR_CMAKE_PLATFORM_UNIX)
  list(APPEND CORDBEE_SOURCES_WKS 
    dactable.cpp
  )
endif(CLR_CMAKE_PLATFORM_UNIX)

if (IS_64BIT_BUILD EQUAL 1)
  list(APPEND CORDBEE_SOURCES_WKS amd64/amd64walker.cpp)
else ()
  list(APPEND CORDBEE_SOURCES_WKS i386/x86walker.cpp)
endif (IS_64BIT_BUILD EQUAL 1)

convert_to_absolute_path(CORDBEE_SOURCES_DAC ${CORDBEE_SOURCES_DAC})
convert_to_absolute_path(CORDBEE_SOURCES_WKS ${CORDBEE_SOURCES_WKS})

set(CORDBEE_DIR ${CMAKE_CURRENT_SOURCE_DIR})

add_subdirectory(dac)
add_subdirectory(wks)
