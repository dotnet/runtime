
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

if (CLR_CMAKE_TARGET_WIN32)
    list(APPEND MDHOTDATA_SOURCES ${MDHOTDATA_HEADERS})
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(mdhotdata_dac ${MDHOTDATA_SOURCES})
set_target_properties(mdhotdata_dac PROPERTIES DAC_COMPONENT TRUE)
target_precompile_headers(mdhotdata_dac PRIVATE external.h)

add_library_clr(mdhotdata_full_obj OBJECT ${MDHOTDATA_SOURCES})
target_precompile_headers(mdhotdata_full_obj PRIVATE external.h)
add_library(mdhotdata_full INTERFACE)
target_sources(mdhotdata_full INTERFACE $<TARGET_OBJECTS:mdhotdata_full_obj>)

if(CLR_CMAKE_HOST_WIN32)
  add_library_clr(mdhotdata-staticcrt ${MDHOTDATA_SOURCES})
  target_precompile_headers(mdhotdata-staticcrt PRIVATE external.h)
endif(CLR_CMAKE_HOST_WIN32)

add_library_clr(mdhotdata_ppdb ${MDHOTDATA_SOURCES})
target_compile_definitions(mdhotdata_ppdb PRIVATE FEATURE_METADATA_EMIT_PORTABLE_PDB)
target_precompile_headers(mdhotdata_ppdb PRIVATE external.h)
