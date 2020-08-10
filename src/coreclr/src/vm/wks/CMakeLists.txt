
if (CLR_CMAKE_TARGET_WIN32)

  if(CLR_CMAKE_HOST_ARCH_ARM OR CLR_CMAKE_HOST_ARCH_ARM64)
    preprocess_compile_asm(TARGET cee_wks_core ASM_FILES ${VM_SOURCES_WKS_ARCH_ASM} OUTPUT_OBJECTS VM_WKS_ARCH_ASM_OBJECTS)
  endif()

endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(cee_wks_core OBJECT ${VM_SOURCES_WKS} ${VM_SOURCES_WKS_ARCH_ASM})
add_library_clr(cee_wks_obj OBJECT ${VM_SOURCES_WKS_SPECIAL})
add_library_clr(cee_wks_mergeable_obj OBJECT ${VM_SOURCES_WKS_SPECIAL})

target_precompile_header(TARGET cee_wks_core HEADER common.h)
target_precompile_header(TARGET cee_wks_obj HEADER common.h)
target_precompile_header(TARGET cee_wks_mergeable_obj HEADER common.h)

if (MSVC)
    # corelib.cpp does not compile with precompiled header file
    set_source_files_properties(../corelib.cpp PROPERTIES COMPILE_FLAGS "/Y-")
endif()

add_dependencies(cee_wks_core eventing_headers)
add_dependencies(cee_wks_obj eventing_headers)
add_dependencies(cee_wks_mergeable_obj eventing_headers)

if (CLR_CMAKE_TARGET_WIN32)

  if(NOT CLR_CMAKE_HOST_ARCH_ARM AND NOT CLR_CMAKE_HOST_ARCH_ARM64)

    if (CLR_CMAKE_HOST_ARCH_I386)
        set_source_files_properties(${VM_SOURCES_WKS_ARCH_ASM} PROPERTIES COMPILE_FLAGS "/Zm /safeseh")
    endif (CLR_CMAKE_HOST_ARCH_I386)

    # Convert AsmConstants.h into AsmConstants.inc
    find_program(POWERSHELL powershell)
    if (POWERSHELL STREQUAL "POWERSHELL-NOTFOUND")
        message(FATAL_ERROR "POWERSHELL not found")
    endif()

    # Get the current list of definitions
    get_compile_definitions(DEFINITIONS)
    add_custom_command(
        OUTPUT "${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.inc"
        DEPENDS ${VM_DIR}/${ARCH_SOURCES_DIR}/asmconstants.h
        COMMAND ${POWERSHELL} -NoProfile -ExecutionPolicy Bypass -NonInteractive \"& \"\"${VM_DIR}/h2inc.ps1\"\"\" \"\"\"${VM_DIR}/${ARCH_SOURCES_DIR}/asmconstants.h\"\"\" >"${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.tmp"
        COMMAND ${CMAKE_CXX_COMPILER} ${DEFINITIONS} /EP "${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.tmp" >"${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.inc"
    )

    set_source_files_properties(${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.inc PROPERTIES GENERATED TRUE)

    add_custom_target(
      asmconstants_inc
      DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.inc
    )

    add_dependencies(cee_wks_core asmconstants_inc)
    add_dependencies(cee_wks_obj asmconstants_inc)
    add_dependencies(cee_wks_mergeable_obj asmconstants_inc)

  endif(NOT CLR_CMAKE_HOST_ARCH_ARM AND NOT CLR_CMAKE_HOST_ARCH_ARM64)

endif (CLR_CMAKE_TARGET_WIN32)

add_custom_target(preprocessd_asm DEPENDS ${VM_WKS_ARCH_ASM_OBJECTS})
add_dependencies(cee_wks_core preprocessd_asm)
add_dependencies(cee_wks_obj preprocessd_asm)
add_dependencies(cee_wks_mergeable_obj preprocessd_asm)


target_compile_definitions(cee_wks_mergeable_obj PUBLIC FEATURE_MERGE_JIT_AND_ENGINE)
target_compile_definitions(cee_wks_mergeable_obj PUBLIC CORECLR_EMBEDDED)

add_library(cee_wks INTERFACE)
target_sources(cee_wks INTERFACE $<TARGET_OBJECTS:cee_wks_obj> ${VM_WKS_ARCH_ASM_OBJECTS})
add_library(cee_wks_mergeable INTERFACE)
target_sources(cee_wks_mergeable INTERFACE $<TARGET_OBJECTS:cee_wks_mergeable_obj> $<TARGET_OBJECTS:cee_wks_core> ${VM_WKS_ARCH_ASM_OBJECTS})
