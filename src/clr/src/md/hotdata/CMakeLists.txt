
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

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(full)
add_subdirectory(crossgen)
if(WIN32)
  add_subdirectory(full-staticcrt)
endif(WIN32)
