
set(MDHOTDATA_SOURCES
  hotmetadata.cpp
  hottable.cpp
  hotheapsdirectoryiterator.cpp
  hotheap.cpp
  hotheapwriter.cpp
)

convert_to_absolute_path(MDHOTDATA_SOURCES ${MDHOTDATA_SOURCES})

if(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

add_subdirectory(dac)
add_subdirectory(full)
if(WIN32)
  add_subdirectory(full-staticcrt)
  add_subdirectory(crossgen)
endif(WIN32)
