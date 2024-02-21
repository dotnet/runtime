add_definitions(-DFEATURE_NO_HOST)

find_nativeaot_library(libhellomanaged REQUIRED)
if ("${libhellomanaged_FOUND}")
  message(STATUS "found libhellomanaged")
else()
  message(FATAL_ERROR "expected libhellomanaged to be found")
endif()
#  include(${CLR_ARTIFACTS_OBJ_DIR}/libhellomanaged/libhellomanaged.cmake OPTIONAL RESULT_VARIABLE INCLUDED_HELLOMANAGED)

include_directories(BEFORE ${VM_DIR})
include_directories(BEFORE ${VM_DIR}/${ARCH_SOURCES_DIR})
include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR})
include_directories(${CLR_DIR}/debug/ee)
include_directories(${CLR_DIR}/gcdump)
include_directories(${CLR_DIR}/interop/inc)

if(CLR_CMAKE_HOST_UNIX)
  include_directories(${GENERATED_INCLUDE_DIR})
endif(CLR_CMAKE_HOST_UNIX)

set(DACCESS_SOURCES
  dacdbiimpl.cpp
  dacdbiimpllocks.cpp
  dacdbiimplstackwalk.cpp
  daccess.cpp
  dacfn.cpp
  enummem.cpp
  fntableaccess.cpp
  gcdump_dac.cpp
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

add_library_clr(daccess ${DACCESS_SOURCES})
set_target_properties(daccess PROPERTIES DAC_COMPONENT TRUE)
target_precompile_headers(daccess PRIVATE [["stdafx.h"]])

if("${libhellomanaged_FOUND}")
  target_link_libraries(daccess PRIVATE libhellomanaged::libhellomanaged)
  target_compile_definitions(daccess PRIVATE HAVE_LIBHELLOMANAGED)
  # FIXME: can we put this with mscordaccore instead?
  install_clr(TARGETS libhellomanaged::libhellomanaged DESTINATIONS . sharedFramework COMPONENT debug)
endif()

add_dependencies(daccess eventing_headers)

if(CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
  add_definitions(-DUSE_DAC_TABLE_RVA)

  set(args $<$<NOT:$<BOOL:${CLR_CMAKE_HOST_OSX}>>:--dynamic> $<TARGET_FILE:coreclr> ${GENERATED_INCLUDE_DIR}/dactablerva.h)

  add_custom_command(
    OUTPUT  ${GENERATED_INCLUDE_DIR}/dactablerva.h
    DEPENDS coreclr
    VERBATIM
    COMMAND_EXPAND_LISTS
    COMMAND ${CMAKE_COMMAND} -E env NM=${CMAKE_NM} ${CLR_DIR}/pal/tools/gen-dactable-rva.sh ${args}
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
endif(CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
