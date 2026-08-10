if (CLR_CMAKE_TARGET_WIN32)
  if(CLR_CMAKE_HOST_ARCH_ARM OR CLR_CMAKE_HOST_ARCH_ARM64)
    preprocess_files(VM_SOURCES_WKS_ARCH_ASM ${VM_SOURCES_WKS_ARCH_ASM})
  endif()
endif (CLR_CMAKE_TARGET_WIN32)

add_library_clr(cee_wks_core OBJECT ${VM_SOURCES_WKS} ${VM_SOURCES_WKS_ARCH_ASM})
add_library_clr(cee_wks OBJECT ${VM_SOURCES_WKS_SPECIAL})
add_library_clr(cee_wks_mergeable OBJECT ${VM_SOURCES_WKS_SPECIAL})

if (CLR_CMAKE_TARGET_ARCH_WASM AND VM_SOURCES_WKS_GEN)
    add_library_clr(cee_wks_gen OBJECT ${VM_SOURCES_WKS_GEN})
endif()

target_precompile_headers(cee_wks_core PRIVATE [["common.h"]])
target_precompile_headers(cee_wks PRIVATE [["common.h"]])
target_precompile_headers(cee_wks_mergeable PRIVATE [["common.h"]])

if (TARGET cee_wks_gen)
    target_precompile_headers(cee_wks_gen PRIVATE [["common.h"]])
endif()

if (MSVC)
    # corelib.cpp does not compile with precompiled header file
    set_source_files_properties(../corelib.cpp PROPERTIES COMPILE_FLAGS "/Y-")
endif()

add_dependencies(cee_wks_core eventing_headers)
add_dependencies(cee_wks eventing_headers)
add_dependencies(cee_wks_mergeable eventing_headers)

if (TARGET cee_wks_gen)
    add_dependencies(cee_wks_gen eventing_headers)
endif()

if (CLR_CMAKE_TARGET_WIN32)
  if(NOT CLR_CMAKE_HOST_ARCH_ARM AND NOT CLR_CMAKE_HOST_ARCH_ARM64)

    if (CLR_CMAKE_HOST_ARCH_I386)
        set_source_files_properties(${VM_SOURCES_WKS_ARCH_ASM} PROPERTIES COMPILE_FLAGS "/Zm /safeseh")
    endif (CLR_CMAKE_HOST_ARCH_I386)

    # Convert AsmConstants.h into AsmConstants.inc
    h2inc("${VM_DIR}/${ARCH_SOURCES_DIR}/asmconstants.h" "${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.tmp")
    preprocess_file("${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.tmp" "${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.inc")

    add_custom_target(asmconstants_inc DEPENDS ${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.inc)

    add_dependencies(cee_wks_core asmconstants_inc)
    add_dependencies(cee_wks asmconstants_inc)
    add_dependencies(cee_wks_mergeable asmconstants_inc)

  endif(NOT CLR_CMAKE_HOST_ARCH_ARM AND NOT CLR_CMAKE_HOST_ARCH_ARM64)
endif (CLR_CMAKE_TARGET_WIN32)

add_custom_target(precompiled_asm DEPENDS ${VM_WKS_ARCH_ASM_OBJECTS})
add_dependencies(cee_wks_core precompiled_asm)
add_dependencies(cee_wks precompiled_asm)
add_dependencies(cee_wks_mergeable precompiled_asm)

if (TARGET cee_wks_gen)
    add_dependencies(cee_wks_gen precompiled_asm)
endif()

target_compile_definitions(cee_wks_mergeable PUBLIC FEATURE_STATICALLY_LINKED)
target_compile_definitions(cee_wks_mergeable PUBLIC CORECLR_EMBEDDED)

target_compile_definitions(cee_wks_core PRIVATE GC_DESCRIPTOR)

if (FEATURE_PERFTRACING AND FEATURE_PERFTRACING_DISABLE_THREADS)
  target_compile_definitions(cee_wks_core PRIVATE PERFTRACING_DISABLE_THREADS)
endif()

if (CLR_CMAKE_HOST_WIN32)
    link_natvis_sources_for_target(cee_wks INTERFACE ../vm.natvis)
    link_natvis_sources_for_target(cee_wks_mergeable INTERFACE ../vm.natvis)
endif()
