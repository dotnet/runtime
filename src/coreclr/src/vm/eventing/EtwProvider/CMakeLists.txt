cmake_minimum_required(VERSION 2.8.12)

include(FindPythonInterp)

set(ETW_PROVIDER_SCRIPT ${CLR_DIR}/src/scripts/genEtwProvider.py)

set (ETW_PROVIDER_OUTPUTS
  ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAll.h
  ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAll.rc
  ${GENERATED_INCLUDE_DIR}/etw/etwmacros.h
  ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAll_MSG00001.bin
  ${GENERATED_INCLUDE_DIR}/etw/ClrEtwAllTEMP.bin
  ${GENERATED_INCLUDE_DIR}/clrxplatevents.h
)

set_source_files_properties(${ETW_PROVIDER_OUTPUTS} PROPERTIES GENERATED TRUE)

add_custom_target(eventprovider
  ${PYTHON_EXECUTABLE} ${ETW_PROVIDER_SCRIPT} --man ${EVENT_MANIFEST} --exc ${EVENT_EXCLUSIONS} --intermediate ${GENERATED_INCLUDE_DIR}
  DEPENDS ${EVENT_MANIFEST} ${EVENT_EXCLUSIONS} ${ETW_PROVIDER_SCRIPT})
