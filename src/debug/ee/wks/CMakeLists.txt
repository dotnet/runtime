if (WIN32)

add_precompiled_header(stdafx.h ../stdafx.cpp CORDBEE_SOURCES_WKS)

get_include_directories(ASM_INCLUDE_DIRECTORIES)
get_compile_definitions(ASM_DEFINITIONS)
set(ASM_OPTIONS /c /Zi /W3 /errorReport:prompt)

if (CLR_CMAKE_PLATFORM_ARCH_I386)
  list (APPEND ASM_OPTIONS /safeseh)
endif (CLR_CMAKE_PLATFORM_ARCH_I386)

set(ASM_FILE ${CORDBEE_DIR}/${ARCH_SOURCES_DIR}/dbghelpers.asm)
# asm files require preprocessing using cl.exe on arm64
if(CLR_CMAKE_PLATFORM_ARCH_ARM64)
    get_filename_component(name ${ASM_FILE} NAME_WE)
    set(ASM_PREPROCESSED_FILE ${CMAKE_CURRENT_BINARY_DIR}/${name}.asm)
    preprocess_def_file(${ASM_FILE} ${ASM_PREPROCESSED_FILE})
    set(CORDBEE_SOURCES_WKS_PREPROCESSED_ASM  ${ASM_PREPROCESSED_FILE})
    set_property(SOURCE ${CORDBEE_SOURCES_WKS_PREPROCESSED_ASM} PROPERTY COMPILE_DEFINITIONS ${ASM_DEFINITIONS})
    set_property(SOURCE ${CORDBEE_SOURCES_WKS_PREPROCESSED_ASM} PROPERTY COMPILE_DEFINITIONS ${ASM_OPTIONS})
    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${CORDBEE_SOURCES_WKS_PREPROCESSED_ASM})
elseif(CLR_CMAKE_PLATFORM_ARCH_ARM)
    
    # On Arm32 for Windows, use C++ compiler to process the .asm since it includes C-style headers.
    set(DBGHELPERS_ASM ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.asm)
    set(ASM_OPTIONS " -g ")
    
    preprocess_def_file(${ASM_FILE} ${DBGHELPERS_ASM})
    
    # We do not pass any defines since we have already done pre-processing above
    set (DBGHELPERS_ASM_CMDLINE "-o ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj ${DBGHELPERS_ASM}")

    file(GENERATE OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/runasm.cmd"
                CONTENT "\"${CMAKE_ASM_MASM_COMPILER}\" ${ASM_OPTIONS} ${DBGHELPERS_ASM_CMDLINE}")

    # Need to compile asm file using custom command as include directories are not provided to asm compiler
    add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj
                       COMMAND ${CMAKE_CURRENT_BINARY_DIR}/runasm.cmd
                       DEPENDS ${DBGHELPERS_ASM}
                       COMMENT "Compiling dbghelpers.asm - ${CMAKE_CURRENT_BINARY_DIR}/runasm.cmd")
    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj)
else ()

    # Need to compile asm file using custom command as include directories are not provided to asm compiler
    add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj
                         COMMAND ${CMAKE_ASM_MASM_COMPILER} ${ASM_INCLUDE_DIRECTORIES} ${ASM_DEFINITIONS} ${ASM_OPTIONS} /Fo${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj /Ta${ASM_FILE}
                         DEPENDS ${ASM_FILE}
                         COMMENT "Compiling dbghelpers.asm")

    add_library_clr(cordbee_wks ${CORDBEE_SOURCES_WKS} ${CMAKE_CURRENT_BINARY_DIR}/dbghelpers.obj)
endif(CLR_CMAKE_PLATFORM_ARCH_ARM64)

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
