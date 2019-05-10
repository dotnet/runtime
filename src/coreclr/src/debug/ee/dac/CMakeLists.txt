
include(${CLR_DIR}/dac.cmake)

add_precompiled_header(stdafx.h ../stdafx.cpp CORDBEE_SOURCES_DAC)

if (WIN32)
  list(APPEND CORDBEE_SOURCES_DAC ${CORDBEE_HEADERS_DAC})
endif (WIN32)

add_library_clr(cordbee_dac ${CORDBEE_SOURCES_DAC})
add_dependencies(cordbee_dac eventing_headers)
