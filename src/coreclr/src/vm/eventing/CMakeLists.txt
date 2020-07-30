set(EVENT_MANIFEST ${VM_DIR}/ClrEtwAll.man)
set(EVENT_EXCLUSIONS ${VM_DIR}/ClrEtwAllMeta.lst)

if (CLR_CMAKE_HOST_WIN32)
  set(NONEXTERN_ARG "--nonextern")
  set(NOXPLATHEADER_ARG "--noxplatheader")
else()
  set(NEED_XPLAT_HEADER ON)
endif()

include(FindPythonInterp)

set (EventingHeaders
  ${GENERATED_INCLUDE_DIR}/etmdummy.h
  ${GENERATED_INCLUDE_DIR}/clretwallmain.h
  ${GENERATED_INCLUDE_DIR}/clreventpipewriteevents.h
  ${GENERATED_INCLUDE_DIR}/clrproviders.h
)

if (NEED_XPLAT_HEADER)
  list(APPEND EventingHeaders
    ${GENERATED_INCLUDE_DIR}/clrxplatevents.h)
endif()

set(GENEVENTING_SCRIPT ${CLR_DIR}/src/scripts/genEventing.py)

add_custom_target(eventing_headers
  ${PYTHON_EXECUTABLE} ${GENEVENTING_SCRIPT} --man ${EVENT_MANIFEST} --inc ${GENERATED_INCLUDE_DIR} --dummy ${GENERATED_INCLUDE_DIR}/etmdummy.h ${NONEXTERN_ARG} ${NOXPLATHEADER_ARG}
  DEPENDS ${EVENT_MANIFEST} ${GENEVENTING_SCRIPT}
  VERBATIM
)

set_source_files_properties(${EventingHeaders} PROPERTIES GENERATED TRUE)

add_dependencies(eventing_headers eventprovider)

add_subdirectory(eventpipe)

if(CLR_CMAKE_HOST_WIN32)
  add_subdirectory(EtwProvider)
endif()
