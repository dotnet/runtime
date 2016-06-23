set(CMAKE_INCLUDE_CURRENT_DIR ON)

include_directories(BEFORE ${CMAKE_CURRENT_SOURCE_DIR}) 
include_directories(BEFORE ${CLR_DIR}/src/vm)
include_directories(BEFORE ${CLR_DIR}/src/vm/${ARCH_SOURCES_DIR})

if(CLR_CMAKE_PLATFORM_UNIX)
  add_compile_options(-fPIC)
endif(CLR_CMAKE_PLATFORM_UNIX)

if(CMAKE_CONFIGURATION_TYPES)
  foreach (Config DEBUG CHECKED)  
     set_property(DIRECTORY APPEND PROPERTY COMPILE_DEFINITIONS $<$<CONFIG:${Config}>:WRITE_BARRIER_CHECK=1>)
  endforeach (Config)
else()
  if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
    add_definitions(-DWRITE_BARRIER_CHECK=1)
  endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
endif(CMAKE_CONFIGURATION_TYPES)

set( GC_SOURCES_DAC_AND_WKS_COMMON
  gccommon.cpp
  gcscan.cpp
  gcsvr.cpp
  gcwks.cpp
  handletable.cpp
  handletablecore.cpp
  handletablescan.cpp
  objecthandle.cpp
  softwarewritewatch.cpp)

set( GC_SOURCES_WKS
  ${GC_SOURCES_DAC_AND_WKS_COMMON}
  gceesvr.cpp
  gceewks.cpp
  handletablecache.cpp)

set( GC_SOURCES_DAC
  ${GC_SOURCES_DAC_AND_WKS_COMMON})

convert_to_absolute_path(GC_SOURCES_WKS ${GC_SOURCES_WKS})
convert_to_absolute_path(GC_SOURCES_DAC ${GC_SOURCES_DAC})

add_subdirectory(wks)
add_subdirectory(dac)
