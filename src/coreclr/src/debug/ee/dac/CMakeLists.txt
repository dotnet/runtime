
if (CLR_CMAKE_TARGET_WIN32)
  list(APPEND CORDBEE_SOURCES_DAC ${CORDBEE_HEADERS_DAC})
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(cordbee_dac ${CORDBEE_SOURCES_DAC})
set_target_properties(cordbee_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_header(TARGET cordbee_dac HEADER stdafx.h)
add_dependencies(cordbee_dac eventing_headers)
