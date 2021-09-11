if (CLR_CMAKE_TARGET_WIN32)

  list(APPEND CORDBEE_SOURCES_WKS ${CORDBEE_HEADERS_WKS})

  get_include_directories(ASM_INCLUDE_DIRECTORIES)
  get_compile_definitions(ASM_DEFINITIONS)

  set(ASM_FILE ${CORDBEE_DIR}/${ARCH_SOURCES_DIR}/dbghelpers.asm)

  if(CLR_CMAKE_HOST_ARCH_ARM OR CLR_CMAKE_HOST_ARCH_ARM64)

    preprocess_files(ASM_FILE ${ASM_FILE})
    if (CMAKE_GENERATOR MATCHES "Visual Studio")
      compile_asm(TARGET cordbee_wks_obj ASM_FILES ${ASM_FILE} OUTPUT_OBJECTS ASM_OBJECTS)
    endif()

    add_library_clr(cordbee_wks_obj OBJECT ${CORDBEE_SOURCES_WKS} ${ASM_FILE} ${ASM_OBJECTS})

  else ()

    set(ASM_OPTIONS /c /Zi /W3 /errorReport:prompt)

    if (CLR_CMAKE_HOST_ARCH_I386)
      list (APPEND ASM_OPTIONS /safeseh)
    endif (CLR_CMAKE_HOST_ARCH_I386)

    set_source_files_properties(${ASM_FILE} PROPERTIES COMPILE_OPTIONS "${ASM_OPTIONS}")

    add_library_clr(cordbee_wks_obj OBJECT ${CORDBEE_SOURCES_WKS} ${ASM_FILE})

  endif()

else ()

  add_library_clr(cordbee_wks_obj OBJECT ${CORDBEE_SOURCES_WKS} ../${ARCH_SOURCES_DIR}/dbghelpers.S)

endif (CLR_CMAKE_TARGET_WIN32)

target_precompile_headers(cordbee_wks_obj PRIVATE [["stdafx.h"]])
add_dependencies(cordbee_wks_obj eventing_headers)
add_library(cordbee_wks INTERFACE)
target_sources(cordbee_wks INTERFACE $<TARGET_OBJECTS:cordbee_wks_obj>)
