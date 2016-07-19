add_definitions(-DFEATURE_METADATA_CUSTOM_DATA_SOURCE -DFEATURE_METADATA_DEBUGGEE_DATA_SOURCE -DFEATURE_NO_HOST -DFEATURE_METADATA_LOAD_TRUSTED_IMAGES)

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

if(WIN32)
    #use static crt
    add_definitions(-MT) 

    if (CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM64)
        set(CORDBDI_SOURCES_ASM_FILE ${ARCH_SOURCES_DIR}/floatconversion.asm)
    endif()
    if (CLR_CMAKE_TARGET_ARCH_AMD64)
        set(CORDBDI_SOURCES
          ${CORDBDI_SOURCES}
          ${CORDBDI_SOURCES_ASM_FILE}
        )
    elseif (CLR_CMAKE_TARGET_ARCH_ARM64 AND NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
        convert_to_absolute_path(CORDBDI_SOURCES_ASM_FILE ${CORDBDI_SOURCES_ASM_FILE})
        get_compile_definitions(ASM_DEFINITIONS)
        set(ASM_OPTIONS /c /Zi /W3 /errorReport:prompt)
        # asm files require preprocessing using cl.exe on arm64
        get_filename_component(name ${CORDBDI_SOURCES_ASM_FILE} NAME_WE)
        set(ASM_PREPROCESSED_FILE ${CMAKE_CURRENT_BINARY_DIR}/${name}.asm)
        preprocess_def_file(${CORDBDI_SOURCES_ASM_FILE} ${ASM_PREPROCESSED_FILE})
        set(CORDBDI_SOURCES_WKS_PREPROCESSED_ASM  ${ASM_PREPROCESSED_FILE})

        set_property(SOURCE ${CORDBDI_SOURCES_WKS_PREPROCESSED_ASM} PROPERTY COMPILE_DEFINITIONS ${ASM_DEFINITIONS})
        set_property(SOURCE ${CORDBDI_SOURCES_WKS_PREPROCESSED_ASM} PROPERTY COMPILE_DEFINITIONS ${ASM_OPTIONS})
        set(CORDBDI_SOURCES
              ${CORDBDI_SOURCES}
              ${CORDBDI_SOURCES_WKS_PREPROCESSED_ASM}
            )
    endif()
elseif(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)

    if(CLR_CMAKE_TARGET_ARCH_AMD64)
      set(CORDBDI_SOURCES
        ${CORDBDI_SOURCES}
        ${ARCH_SOURCES_DIR}/floatconversion.S
      )
    endif()

endif(WIN32)

add_precompiled_header(stdafx.h stdafx.cpp CORDBDI_SOURCES)


add_library_clr(cordbdi STATIC ${CORDBDI_SOURCES})
