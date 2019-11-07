add_definitions(
    -DFEATURE_METADATA_CUSTOM_DATA_SOURCE
    -DFEATURE_METADATA_DEBUGGEE_DATA_SOURCE
    -DFEATURE_NO_HOST
    -DFEATURE_METADATA_LOAD_TRUSTED_IMAGES)

set(CORDBDI_SOURCES
  shimprocess.cpp
  shimcallback.cpp
  shimevents.cpp
  shimdatatarget.cpp
  shimstackwalk.cpp
  breakpoint.cpp
  cordb.cpp
  divalue.cpp
  dbgtransportmanager.cpp
  hash.cpp
  module.cpp
  nativepipeline.cpp
  platformspecific.cpp
  process.cpp
  rsappdomain.cpp
  rsassembly.cpp
  rsclass.cpp
  rsfunction.cpp
  rsmain.cpp
  rsmda.cpp
  rsregsetcommon.cpp
  rsstackwalk.cpp
  rsthread.cpp
  rstype.cpp
  shared.cpp
  symbolinfo.cpp
  valuehome.cpp
)

set(CORDBDI_HEADERS
    shimpriv.h
    shimdatatarget.h
    classfactory.h
    dbgtransportmanager.h
    eventchannel.h
    helpers.h
    nativepipeline.h
    rsenumerator.hpp
    rspriv.h
    rspriv.inl
    symbolinfo.h)


if(WIN32)
    #use static crt
    add_definitions(-MT)

    if (CLR_CMAKE_TARGET_ARCH_AMD64 OR ((CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_ARM)
        AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD))
        set(CORDBDI_SOURCES_ASM_FILE ${ARCH_SOURCES_DIR}/floatconversion.asm)
    endif()

    if ((CLR_CMAKE_TARGET_ARCH_ARM OR CLR_CMAKE_TARGET_ARCH_ARM64) AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
        convert_to_absolute_path(CORDBDI_SOURCES_ASM_FILE ${CORDBDI_SOURCES_ASM_FILE})
        preprocess_compile_asm(ASM_FILES ${CORDBDI_SOURCES_ASM_FILE} OUTPUT_OBJECTS CORDBDI_SOURCES_ASM_FILE)
    endif()
elseif(CLR_CMAKE_PLATFORM_UNIX)

    if(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64 OR CLR_CMAKE_TARGET_ARCH_ARM)
      set(CORDBDI_SOURCES_ASM_FILE
        ${ARCH_SOURCES_DIR}/floatconversion.S
      )
    endif()

endif(WIN32)

if (WIN32)
    convert_to_absolute_path(CORDBDI_HEADERS ${CORDBDI_HEADERS})
    list(APPEND CORDBDI_SOURCES ${CORDBDI_HEADERS})
endif (WIN32)

list(APPEND CORDBDI_SOURCES ${CORDBDI_SOURCES_ASM_FILE})

add_library_clr(cordbdi STATIC ${CORDBDI_SOURCES})
target_precompile_header(TARGET cordbdi HEADER stdafx.h)
add_dependencies(cordbdi eventing_headers)
