if (WIN32)

  add_precompiled_header(stdafx.h ../stdafx.cpp CORDBEE_SOURCES_WKS)

  list(APPEND CORDBEE_SOURCES_WKS ${CORDBEE_HEADERS_WKS})

  get_include_directories(ASM_INCLUDE_DIRECTORIES)
  get_compile_definitions(ASM_DEFINITIONS)

  set(ASM_FILE ${CORDBEE_DIR}/${ARCH_SOURCES_DIR}/dbghelpers.asm)

  if(CLR_CMAKE_PLATFORM_ARCH_ARM OR CLR_CMAKE_PLATFORM_ARCH_ARM64)

    get_filename_component(name ${ASM_FILE} NAME_WE)
    file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/${name}.asm" ASM_PREPROCESSED_FILE)
    preprocess_def_file(${ASM_FILE} ${ASM_PREPROCESSED_FILE})

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

    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj)

  else ()

    set(ASM_OPTIONS /c /Zi /W3 /errorReport:prompt)

    if (CLR_CMAKE_PLATFORM_ARCH_I386)
      list (APPEND ASM_OPTIONS /safeseh)
    endif (CLR_CMAKE_PLATFORM_ARCH_I386)

    # Need to compile asm file using custom command as include directories are not provided to asm compiler
    add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj
                       COMMAND ${CMAKE_ASM_MASM_COMPILER} ${ASM_INCLUDE_DIRECTORIES} ${ASM_DEFINITIONS} ${ASM_OPTIONS} /Fo${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj /Ta${ASM_FILE}
                       DEPENDS ${ASM_FILE}
                       COMMENT "Compiling dbghelpers.asm")

    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj)

  endif()

else ()

  add_compile_options(-fPIC)

  if(CLR_CMAKE_PLATFORM_ARCH_AMD64 OR CLR_CMAKE_PLATFORM_ARCH_ARM OR CLR_CMAKE_PLATFORM_ARCH_ARM64 OR CLR_CMAKE_PLATFORM_ARCH_I386)
    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ../${ARCH_SOURCES_DIR}/dbghelpers.S)
  else()
    message(FATAL_ERROR "Unknown platform")
  endif()

endif (WIN32)

add_dependencies(cordbee_wks eventing_headers)
