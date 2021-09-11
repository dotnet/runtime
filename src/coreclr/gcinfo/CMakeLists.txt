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

install (FILES gcinfoencoder.cpp
               DESTINATION gcinfo)

function(create_gcinfo_lib)

  set(oneValueArgs TARGET OS ARCH)
  cmake_parse_arguments(TARGETDETAILS "${options}" "${oneValueArgs}" "" ${ARGN})

  set( GCINFO_SOURCES
    ${GCINFO_ALLARCH_SOURCES}
  )

  if(TARGETDETAILS_ARCH STREQUAL "x86")
    list(APPEND GCINFO_SOURCES
      ${GCINFO_I386_SOURCES}
    )
  endif(TARGETDETAILS_ARCH STREQUAL "x86")

  add_library_clr("${TARGETDETAILS_TARGET}"
    STATIC
    ${GCINFO_SOURCES}
  )

  set_target_definitions_to_custom_os_and_arch(${ARGN})
endfunction()

if (CLR_CMAKE_TARGET_UNIX)
set(TARGET_OS_NAME unix)
else()
set(TARGET_OS_NAME win)
endif()

if (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)
  create_gcinfo_lib(TARGET gcinfo_unix_arm64 OS unix ARCH arm64)
  create_gcinfo_lib(TARGET gcinfo_unix_x64 OS unix ARCH x64)
  create_gcinfo_lib(TARGET gcinfo_win_arm64 OS win ARCH arm64)
  create_gcinfo_lib(TARGET gcinfo_win_x64 OS win ARCH x64)
endif (CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_AMD64)

create_gcinfo_lib(TARGET gcinfo_unix_armel OS unix ARCH armel)
create_gcinfo_lib(TARGET gcinfo_unix_arm OS unix ARCH arm)
create_gcinfo_lib(TARGET gcinfo_win_arm OS win ARCH arm)
create_gcinfo_lib(TARGET gcinfo_win_x86 OS win ARCH x86)

if (CLR_CMAKE_TARGET_ARCH_I386 AND CLR_CMAKE_TARGET_UNIX)
  create_gcinfo_lib(TARGET gcinfo_unix_x86 OS unix ARCH x86)
endif (CLR_CMAKE_TARGET_ARCH_I386 AND CLR_CMAKE_TARGET_UNIX)
