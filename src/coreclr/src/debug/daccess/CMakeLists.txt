
include(${CLR_DIR}/dac.cmake)

add_definitions(-DFEATURE_NO_HOST)

include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${CLR_DIR}/src/debug/ee)
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

add_dependencies(daccess eventing_headers)

if(CLR_CMAKE_PLATFORM_UNIX)
  add_custom_command(
    OUTPUT  ${GENERATED_INCLUDE_DIR}/dactablerva.h
    DEPENDS coreclr
    VERBATIM
    COMMAND sh ${CLR_DIR}/src/pal/tools/gen-dactable-rva.sh $<TARGET_FILE:coreclr> ${GENERATED_INCLUDE_DIR}/dactablerva.h
    COMMENT Generating ${GENERATED_INCLUDE_DIR}/dactablerva.h
  )

  set_source_files_properties(
    ${GENERATED_INCLUDE_DIR}/dactablerva.h
    PROPERTIES GENERATED TRUE
  )

  add_custom_target(
    dactablerva_header
    DEPENDS ${GENERATED_INCLUDE_DIR}/dactablerva.h
  )

  add_dependencies(daccess dactablerva_header)
endif(CLR_CMAKE_PLATFORM_UNIX)
