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

function(remove_standard_gcinfo_targetdetails)
  if (CLR_CMAKE_TARGET_ARCH_I386)
    remove_definitions(-DTARGET_X86)
  elseif(CLR_CMAKE_TARGET_ARCH_AMD64)
    remove_definitions(-DTARGET_64BIT)
    remove_definitions(-DTARGET_AMD64)
  elseif(CLR_CMAKE_TARGET_ARCH_ARM64)
    remove_definitions(-DTARGET_64BIT)
    remove_definitions(-DTARGET_ARM64)
    remove_definitions(-DFEATURE_MULTIREG_RETURN)
  elseif(CLR_CMAKE_TARGET_ARCH_ARM)
    remove_definitions(-DTARGET_ARM)
  endif()

  if (CLR_CMAKE_TARGET_UNIX)
    if (CLR_CMAKE_TARGET_ARCH_AMD64)
      remove_definitions(-DUNIX_AMD64_ABI)
    elseif (CLR_CMAKE_TARGET_ARCH_ARM)
      remove_definitions(-DUNIX_ARM_ABI)
    elseif (CLR_CMAKE_TARGET_ARCH_I386)
      remove_definitions(-DUNIX_X86_ABI)
    endif()
  endif(CLR_CMAKE_TARGET_UNIX)

  if(CLR_CMAKE_TARGET_UNIX_AMD64)
    remove_definitions(-DFEATURE_MULTIREG_RETURN)
  endif (CLR_CMAKE_TARGET_UNIX_AMD64)

  if (NOT CLR_CMAKE_TARGET_ARCH_I386 OR NOT CLR_CMAKE_TARGET_WIN32)
    remove_definitions(-DFEATURE_EH_FUNCLETS)
  endif (NOT CLR_CMAKE_TARGET_ARCH_I386 OR NOT CLR_CMAKE_TARGET_WIN32)
endfunction()

function(set_gcinfo_targetdetails)

  set(options ARM ARM64 I386 AMD64 UNIX WIN32)
  set(oneValueArgs GCINFONAME)
  cmake_parse_arguments(TARGETDETAILS "${options}" "${oneValueArgs}" "" ${ARGN})

  remove_standard_gcinfo_targetdetails()

  set( GCINFO_SOURCES
    ${GCINFO_ALLARCH_SOURCES}
  )

  if(TARGETDETAILS_I386)
    list(APPEND GCINFO_SOURCES
      ${GCINFO_I386_SOURCES}
    )
  endif(TARGETDETAILS_I386)

  if (TARGETDETAILS_I386)
    add_definitions(-DTARGET_X86)
  elseif(TARGETDETAILS_AMD64)
    add_definitions(-DTARGET_64BIT)
    add_definitions(-DTARGET_AMD64)
  elseif(TARGETDETAILS_ARM64)
    add_definitions(-DTARGET_64BIT)
    add_definitions(-DTARGET_ARM64)
    add_definitions(-DFEATURE_MULTIREG_RETURN)
  elseif(TARGETDETAILS_ARM)
    add_definitions(-DTARGET_ARM)
  endif()

  if (TARGETDETAILS_UNIX)
    if (TARGETDETAILS_AMD64)
      add_definitions(-DUNIX_AMD64_ABI)
      add_definitions(-DFEATURE_MULTIREG_RETURN)
    elseif (TARGETDETAILS_ARM)
      add_definitions(-DUNIX_ARM_ABI)
    elseif (TARGETDETAILS_I386)
      add_definitions(-DUNIX_X86_ABI)
    elseif (TARGETDETAILS_ARM64)
    endif()
  endif(TARGETDETAILS_UNIX)

  if (NOT TARGETDETAILS_I386 OR NOT TARGETDETAILS_WIN32)
    add_definitions(-DFEATURE_EH_FUNCLETS)
  endif (NOT TARGETDETAILS_I386 OR NOT TARGETDETAILS_WIN32)

  add_library_clr("${TARGETDETAILS_GCINFONAME}"
    STATIC
    ${GCINFO_SOURCES}
  )
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