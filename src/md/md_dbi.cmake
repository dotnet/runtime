add_definitions(-DFEATURE_METADATA_EMIT)
add_definitions(-DFEATURE_METADATA_EMIT_IN_DEBUGGER)
add_definitions(-DFEATURE_METADATA_INTERNAL_APIS)
add_definitions(-DFEATURE_METADATA_CUSTOM_DATA_SOURCE)
add_definitions(-DFEATURE_METADATA_DEBUGGEE_DATA_SOURCE)
# Enable mscordbi-only (perf) feature -->
add_definitions(-DFEATURE_METADATA_LOAD_TRUSTED_IMAGES -DFEATURE_METADATA_RELEASE_MEMORY_ON_REOPEN)


if(WIN32)
    # using static crt for dbi
    if (CMAKE_BUILD_TYPE STREQUAL DEBUG)
      add_definitions(-MTd) 
    else()
      add_definitions(-MT) 
    endif(CMAKE_BUILD_TYPE STREQUAL DEBUG)
endif(WIN32)
