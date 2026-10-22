include_directories("../../inc")

if(FEATURE_GDBJIT)
   add_definitions(-DFEATURE_GDBJIT)
endif(FEATURE_GDBJIT)

set(CLR_SOURCES
    mscoree.cpp
    exports.cpp
)

if(CLR_CMAKE_TARGET_WIN32)
list(APPEND CLR_SOURCES
  Native.rc
)
set (DEF_SOURCES
  mscorwks_ntdef.src
)
else()
set (DEF_SOURCES
  mscorwks_unixexports.src
)
if(CLR_CMAKE_ENABLE_OBJECT_HEADER_BITS_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_objectheaderbitstest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_LXR_WORK_PACKET_SCHEDULER_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_lxrworkpacketschedulertest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_LXR_PAUSE_COORDINATOR_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_lxrpausecoordinatortest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_LXR_DEFERRED_BUCKET_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_lxrdeferredbuckettest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_LXR_LAZY_COMPLETION_COUNTER_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_lxrcompletioncountertest_unixexports.src
  )
endif()
if(FEATURE_GDBJIT)
  list(APPEND DEF_SOURCES
    gdbjit_unixexports.src
  )
endif(FEATURE_GDBJIT)
if(CLR_CMAKE_ENABLE_IMMIX_BLOCK_STATE_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_immixblockstatetest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_LARGE_OBJECT_SPACE_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_largeobjectspacetest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_IMMIX_ALLOCATION_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_immixallocationtest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_REFERENCE_COUNT_TEST)
  list(APPEND DEF_SOURCES
    mscorwks_referencecounttest_unixexports.src
  )
endif()
if(CLR_CMAKE_ENABLE_LXR_RC_VERTICAL)
  list(APPEND DEF_SOURCES mscorwks_lxrrcverticaltest_unixexports.src)
endif()
if(CLR_CMAKE_ENABLE_IMMIX_BLOCK_STATE_TEST OR
   CLR_CMAKE_ENABLE_LARGE_OBJECT_SPACE_TEST OR
   CLR_CMAKE_ENABLE_IMMIX_ALLOCATION_TEST OR
   CLR_CMAKE_ENABLE_REFERENCE_COUNT_TEST OR
   CLR_CMAKE_ENABLE_LXR_WORK_PACKET_SCHEDULER_TEST OR
   CLR_CMAKE_ENABLE_LXR_DEFERRED_BUCKET_TEST OR
   CLR_CMAKE_ENABLE_LXR_LAZY_COMPLETION_COUNTER_TEST OR
   CLR_CMAKE_ENABLE_LXR_PAUSE_COORDINATOR_TEST OR CLR_CMAKE_ENABLE_LXR_RC_VERTICAL)
  convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})
  set_property(DIRECTORY APPEND PROPERTY CMAKE_CONFIGURE_DEPENDS ${DEF_SOURCES})
  set(COMBINED_DEF_SOURCE
    ${CMAKE_CURRENT_BINARY_DIR}/mscorwks_validation_combined_unixexports.src)
  file(WRITE ${COMBINED_DEF_SOURCE} "")
  foreach(DEF_SOURCE IN LISTS DEF_SOURCES)
    file(READ ${DEF_SOURCE} DEF_SOURCE_CONTENT)
    file(APPEND ${COMBINED_DEF_SOURCE} "${DEF_SOURCE_CONTENT}\n")
  endforeach()
  set(DEF_SOURCES ${COMBINED_DEF_SOURCE})
endif()
endif(CLR_CMAKE_TARGET_WIN32)

convert_to_absolute_path(DEF_SOURCES ${DEF_SOURCES})
convert_to_absolute_path(CLR_SOURCES ${CLR_SOURCES})
add_subdirectory(coreclr)
