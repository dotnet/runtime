if (WIN32)

  list(APPEND CORDBEE_SOURCES_WKS ${CORDBEE_HEADERS_WKS})

  get_include_directories(ASM_INCLUDE_DIRECTORIES)
  get_compile_definitions(ASM_DEFINITIONS)

  set(ASM_FILE ${CORDBEE_DIR}/${ARCH_SOURCES_DIR}/dbghelpers.asm)

  if(CLR_CMAKE_PLATFORM_ARCH_ARM OR CLR_CMAKE_PLATFORM_ARCH_ARM64)

    preprocess_compile_asm(ASM_FILES ${ASM_FILE} OUTPUT_OBJECTS ASM_OBJECTS)

    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${ASM_OBJECTS})

  else ()

    set(ASM_OPTIONS /c /Zi /W3 /errorReport:prompt)

    if (CLR_CMAKE_PLATFORM_ARCH_I386)
      list (APPEND ASM_OPTIONS /safeseh)
    endif (CLR_CMAKE_PLATFORM_ARCH_I386)

    set_source_files_properties(${ASM_FILE} PROPERTIES COMPILE_OPTIONS "${ASM_OPTIONS}")

    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${ASM_FILE})

  endif()

else ()

  if(CLR_CMAKE_PLATFORM_ARCH_AMD64 OR CLR_CMAKE_PLATFORM_ARCH_ARM OR CLR_CMAKE_PLATFORM_ARCH_ARM64 OR CLR_CMAKE_PLATFORM_ARCH_I386)
    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ../${ARCH_SOURCES_DIR}/dbghelpers.S)
  else()
    message(FATAL_ERROR "Unknown platform")
  endif()

endif (WIN32)

target_precompile_header(TARGET cordbee_wks HEADER stdafx.h)
add_dependencies(cordbee_wks eventing_headers)
