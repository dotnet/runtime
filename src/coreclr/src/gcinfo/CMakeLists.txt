set(CMAKE_INCLUDE_CURRENT_DIR ON)

set( GCINFO_ALLARCH_SOURCES
  arraylist.cpp
  gcinfoencoder.cpp
  simplerhash.cpp
)

set( GCINFO_I386_SOURCES
  ../gcdump/gcdump.cpp
  ../gcdump/i386/gcdumpx86.cpp
)

convert_to_absolute_path(GCINFO_ALLARCH_SOURCES ${GCINFO_ALLARCH_SOURCES})
convert_to_absolute_path(GCINFO_I386_SOURCES ${GCINFO_I386_SOURCES})

set( GCINFO_SOURCES
  ${GCINFO_ALLARCH_SOURCES}
  )
if(CLR_CMAKE_TARGET_ARCH_I386)
  list(APPEND GCINFO_SOURCES
    ${GCINFO_I386_SOURCES}
  )
endif(CLR_CMAKE_TARGET_ARCH_I386)


add_library_clr(gcinfo_obj
    OBJECT
    ${GCINFO_SOURCES}
)

add_library(gcinfo INTERFACE)
target_sources(gcinfo INTERFACE $<TARGET_OBJECTS:gcinfo_obj>)

add_library_clr(gcinfo_crossgen
    STATIC
    ${GCINFO_SOURCES}
)

set_target_properties(gcinfo_crossgen PROPERTIES CROSSGEN_COMPONENT TRUE)

_install (FILES gcinfoencoder.cpp
               DESTINATION gcinfo)

function(set_gcinfo_targetdetails)

  set(options ARM ARM64 I386 AMD64 UNIX WIN32)
  set(oneValueArgs TARGET)
  cmake_parse_arguments(TARGETDETAILS "${options}" "${oneValueArgs}" "" ${ARGN})

  set( GCINFO_SOURCES
    ${GCINFO_ALLARCH_SOURCES}
  )

  if(TARGETDETAILS_I386)
    list(APPEND GCINFO_SOURCES
      ${GCINFO_I386_SOURCES}
    )
  endif(TARGETDETAILS_I386)

  add_library_clr("${TARGETDETAILS_TARGET}"
    STATIC
    ${GCINFO_SOURCES}
  )

  set_target_definitions_to_custom_os_and_arch(${ARGN})
endfunction()

if (CLR_CMAKE_BUILD_SUBSET_ALLJITS AND NOT CLR_CROSS_COMPONENTS_BUILD)
  if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
    add_subdirectory(gcinfo_win_arm64)
    add_subdirectory(gcinfo_win_x64)
    add_subdirectory(gcinfo_unix_arm64)
    add_subdirectory(gcinfo_unix_x64)
  endif (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  add_subdirectory(gcinfo_unix_arm)
  add_subdirectory(gcinfo_win_arm)
  add_subdirectory(gcinfo_win_x86)
else()
  if (CLR_CMAKE_TARGET_UNIX)
    add_subdirectory(gcinfo_unix_${ARCH_TARGET_NAME})
  endif(CLR_CMAKE_TARGET_UNIX)
endif (CLR_CMAKE_BUILD_SUBSET_ALLJITS AND NOT CLR_CROSS_COMPONENTS_BUILD)