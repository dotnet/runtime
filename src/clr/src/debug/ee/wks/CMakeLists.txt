if (WIN32)

add_precompiled_header(stdafx.h ../stdafx.cpp CORDBEE_SOURCES_WKS)

get_include_directories(ASM_INCLUDE_DIRECTORIES)
get_compile_definitions(ASM_DEFINITIONS)
set(ASM_OPTIONS /c /Zi /W3 /errorReport:prompt)

if (CLR_CMAKE_PLATFORM_ARCH_I386)
  list (APPEND ASM_OPTIONS /safeseh)
endif (CLR_CMAKE_PLATFORM_ARCH_I386)

# Need to compile asm file using custom command as include directories are not provided to asm compiler
add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj
                   COMMAND ${CMAKE_ASM_MASM_COMPILER} ${ASM_INCLUDE_DIRECTORIES} ${ASM_DEFINITIONS} ${ASM_OPTIONS} /Fo${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj /Ta${CORDBEE_DIR}/${ARCH_SOURCES_DIR}/dbghelpers.asm
                   DEPENDS ${CORDBEE_DIR}/${ARCH_SOURCES_DIR}/dbghelpers.asm
                   COMMENT "Compiling dbghelpers.asm")

if(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  #mark obj as source that does not require compile
  set_source_files_properties(${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj PROPERTIES EXTERNAL_OBJECT TRUE)
  add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS})
else()
  add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj)
endif()

else ()

add_compile_options(-fPIC)

if(CLR_CMAKE_PLATFORM_ARCH_AMD64)
  add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ../${ARCH_SOURCES_DIR}/dbghelpers.S)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
  add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ../${ARCH_SOURCES_DIR}/dbghelpers.S)
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
  add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS})
else()
  message(FATAL_ERROR "Only ARM and AMD64 is supported")
endif()

endif (WIN32)
