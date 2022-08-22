set(CMAKE_INCLUDE_CURRENT_DIR ON)

add_definitions(-DFEATURE_NO_HOST)

include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})

set(CORDBEE_SOURCES_DAC_AND_WKS
  controller.cpp
  debugger.cpp
  debuggermessagebox.cpp
  debuggermodule.cpp
  functioninfo.cpp
)

set(CORDBEE_HEADERS_DAC_AND_WKS
  controller.h
  controller.inl
  datatest.h
  debugger.h
  debugger.inl
)

set(CORDBEE_SOURCES_WKS
  ${CORDBEE_SOURCES_DAC_AND_WKS}
  funceval.cpp
  rcthread.cpp
  canary.cpp
  shared.cpp
  frameinfo.cpp
  dactable.cpp
  ${ARCH_SOURCES_DIR}/primitives.cpp
)

set(CORDBEE_HEADERS_WKS
  ${CORDBEE_HEADERS_DAC_AND_WKS}
  canary.h
  frameinfo.h
)

set(CORDBEE_SOURCES_DAC
  ${CORDBEE_SOURCES_DAC_AND_WKS}
)

set(CORDBEE_HEADERS_DAC
  ${CORDBEE_HEADERS_DAC_AND_WKS}
)

if(CLR_CMAKE_TARGET_ARCH_AMD64)
  list(APPEND CORDBEE_SOURCES_WKS
    ${ARCH_SOURCES_DIR}/debuggerregdisplayhelper.cpp
    ${ARCH_SOURCES_DIR}/amd64walker.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_I386)
  list(APPEND CORDBEE_SOURCES_WKS
    ${ARCH_SOURCES_DIR}/debuggerregdisplayhelper.cpp
    ${ARCH_SOURCES_DIR}/x86walker.cpp
  )
elseif(CLR_CMAKE_TARGET_ARCH_ARM)
  list(APPEND CORDBEE_SOURCES_WKS ${ARCH_SOURCES_DIR}/armwalker.cpp)
elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
  list(APPEND CORDBEE_SOURCES_WKS ${ARCH_SOURCES_DIR}/arm64walker.cpp)
elseif(CLR_CMAKE_TARGET_ARCH_LOONGARCH64)
  list(APPEND CORDBEE_SOURCES_WKS ${ARCH_SOURCES_DIR}/loongarch64walker.cpp)
endif()

convert_to_absolute_path(CORDBEE_SOURCES_DAC ${CORDBEE_SOURCES_DAC})
convert_to_absolute_path(CORDBEE_SOURCES_WKS ${CORDBEE_SOURCES_WKS})

if (CLR_CMAKE_TARGET_WIN32)
  convert_to_absolute_path(CORDBEE_HEADERS_DAC ${CORDBEE_HEADERS_DAC})
  convert_to_absolute_path(CORDBEE_HEADERS_WKS ${CORDBEE_HEADERS_WKS})
endif (CLR_CMAKE_TARGET_WIN32)

set(CORDBEE_DIR ${CMAKE_CURRENT_SOURCE_DIR})

add_subdirectory(dac)
add_subdirectory(wks)
