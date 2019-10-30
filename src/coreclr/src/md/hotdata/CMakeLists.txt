
set(MDHOTDATA_SOURCES
  hotmetadata.cpp
  hottable.cpp
  hotheapsdirectoryiterator.cpp
  hotheap.cpp
  hotheapwriter.cpp
)

set(MDHOTDATA_HEADERS
  ../../inc/metamodelpub.h
  ../databuffer.h
  ../heaps/export.h
  ../inc/streamutil.h
  ./export.h
  heapindex.h
  hotdataformat.h
  hotheap.h
  hotheapsdirectoryiterator.h
  hotheapwriter.h
  hotmetadata.h
  hottable.h
)

convert_to_absolute_path(MDHOTDATA_HEADERS ${MDHOTDATA_HEADERS})
convert_to_absolute_path(MDHOTDATA_SOURCES ${MDHOTDATA_SOURCES})

if (WIN32)
    list(APPEND MDHOTDATA_SOURCES ${MDHOTDATA_HEADERS})
endif (WIN32)

add_library_clr(mdhotdata_dac ${MDHOTDATA_SOURCES})
set_target_properties(mdhotdata_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_header(TARGET mdhotdata_dac HEADER external.h)

add_library_clr(mdhotdata_full ${MDHOTDATA_SOURCES})
target_precompile_header(TARGET mdhotdata_full HEADER external.h)

add_library_clr(mdhotdata_crossgen ${MDHOTDATA_SOURCES})
set_target_properties(mdhotdata_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)
target_precompile_header(TARGET mdhotdata_crossgen HEADER external.h)

if(WIN32)
  add_library_clr(mdhotdata-staticcrt ${MDHOTDATA_SOURCES})
  target_precompile_header(TARGET mdhotdata-staticcrt HEADER external.h)
endif(WIN32)
