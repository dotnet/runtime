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

        # Inserts a custom command in CMake build to preprocess each asm source file
        get_filename_component(name ${CORDBDI_SOURCES_ASM_FILE} NAME_WE)
        file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/${name}.asm" ASM_PREPROCESSED_FILE)
        preprocess_def_file(${CORDBDI_SOURCES_ASM_FILE} ${ASM_PREPROCESSED_FILE})

        # We do not pass any defines since we have already done pre-processing above
        set (ASM_CMDLINE "-o ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj ${ASM_PREPROCESSED_FILE}")

        # Generate the batch file that will invoke the assembler
        file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/runasm_${name}.cmd" ASM_SCRIPT_FILE)

        file(GENERATE OUTPUT "${ASM_SCRIPT_FILE}"
            CONTENT "\"${CMAKE_ASM_MASM_COMPILER}\" -g ${ASM_CMDLINE}")

        message("Generated  - ${ASM_SCRIPT_FILE}")

        # Need to compile asm file using custom command as include directories are not provided to asm compiler
        add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj
                            COMMAND ${ASM_SCRIPT_FILE}
                            DEPENDS ${ASM_PREPROCESSED_FILE}
                            COMMENT "Assembling ${ASM_PREPROCESSED_FILE} - ${ASM_SCRIPT_FILE}")

        # mark obj as source that does not require compile
        set_source_files_properties(${CMAKE_CURRENT_BINARY_DIR}/${name}.obj PROPERTIES EXTERNAL_OBJECT TRUE)

        # Add the generated OBJ in the dependency list so that it gets consumed during linkage
        set(CORDBDI_SOURCES_ASM_FILE ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj)
    endif()
elseif(CLR_CMAKE_PLATFORM_UNIX)
    add_compile_options(-fPIC)

    if(CLR_CMAKE_TARGET_ARCH_AMD64 OR CLR_CMAKE_TARGET_ARCH_ARM)
      set(CORDBDI_SOURCES_ASM_FILE
        ${ARCH_SOURCES_DIR}/floatconversion.S
      )
    endif()

endif(WIN32)

add_precompiled_header(stdafx.h stdafx.cpp CORDBDI_SOURCES)

if (WIN32)
    convert_to_absolute_path(CORDBDI_HEADERS ${CORDBDI_HEADERS})
    list(APPEND CORDBDI_SOURCES ${CORDBDI_HEADERS})
endif (WIN32)

list(APPEND CORDBDI_SOURCES ${CORDBDI_SOURCES_ASM_FILE})

add_library_clr(cordbdi STATIC ${CORDBDI_SOURCES})
add_dependencies(cordbdi eventing_headers)
