if (WIN32)
  add_precompiled_header(common.h ../common.cpp VM_SOURCES_WKS)
  # mscorlib.cpp does not compile with precompiled header file
  set_source_files_properties(../mscorlib.cpp PROPERTIES COMPILE_FLAGS "/Y-") 
  # .c file cannot use cpp precompiled header 
  set_source_files_properties(../microsoft.comservices_i.c PROPERTIES COMPILE_FLAGS "/Y-") 

  # asm files require preprocessing using cl.exe on arm64
  if(CLR_CMAKE_PLATFORM_ARCH_ARM64)
      foreach(ASM_FILE ${VM_SOURCES_WKS_ARCH_ASM})
          get_filename_component(name ${ASM_FILE} NAME_WE)
          set(ASM_PREPROCESSED_FILE ${CMAKE_CURRENT_BINARY_DIR}/${name}.asm)
          preprocess_def_file(${ASM_FILE} ${ASM_PREPROCESSED_FILE})
          set(VM_SOURCES_WKS_ARM64_PREPROCESSED_ASM ${VM_SOURCES_WKS_ARM64_PREPROCESSED_ASM} ${ASM_PREPROCESSED_FILE})
      endforeach()

      set(VM_SOURCES_WKS_ARCH_ASM ${VM_SOURCES_WKS_ARM64_PREPROCESSED_ASM})
  endif(CLR_CMAKE_PLATFORM_ARCH_ARM64)
endif (WIN32)

add_library_clr(cee_wks ${VM_SOURCES_WKS} ${VM_SOURCES_WKS_ARCH_ASM})

if (WIN32)
# Get the current list of definitions
get_compile_definitions(DEFINITIONS)

get_directory_property(COMPILE_DEFINITIONS_LIST COMPILE_DEFINITIONS)

# Extract the definitions for the ASM code. Since there is a bug in the cmake that prevents us from
# using the generator expressions, we split the definitions into lists based on the configuration.
foreach(DEFINITION IN LISTS COMPILE_DEFINITIONS_LIST)
    if (${DEFINITION} MATCHES "^\\$<\\$<CONFIG:([^>]+)>:([^>]+)>$")
        # The entry contains generator expression, so insert the definition into a definitions list
        # corresponding to the config
        string(TOUPPER ${CMAKE_MATCH_1} CONFIG)
        set(ASM_DEFINITIONS_${CONFIG} ${ASM_DEFINITIONS_${CONFIG}} ${CMAKE_MATCH_2}) 
    else()
        list(APPEND ASM_DEFINITIONS ${DEFINITION})        
    endif()
endforeach()

# Add defines for the ASM. Unfortunately setting it on the target is ignored for asm by the cmake, so we have
# to set it on the sources.
set_property(SOURCE ${VM_SOURCES_WKS_ARCH_ASM} PROPERTY COMPILE_DEFINITIONS ${ASM_DEFINITIONS})
foreach(CONFIG IN LISTS CMAKE_CONFIGURATION_TYPES)
    string(TOUPPER ${CONFIG} CONFIG)
    set_property(SOURCE ${VM_SOURCES_WKS_ARCH_ASM} PROPERTY COMPILE_DEFINITIONS_${CONFIG} ${ASM_DEFINITIONS_${CONFIG}})
endforeach()

if (CLR_CMAKE_PLATFORM_ARCH_I386)
    set_source_files_properties(${VM_SOURCES_WKS_ARCH_ASM} PROPERTIES COMPILE_FLAGS "/Zm /safeseh")
endif (CLR_CMAKE_PLATFORM_ARCH_I386)

# Convert AsmConstants.h into AsmConstants.inc
find_program(POWERSHELL powershell)
if (POWERSHELL STREQUAL "POWERSHELL-NOTFOUND")
    message(FATAL_ERROR "POWERSHELL not found")
endif()

add_custom_command(
    # The AsmConstants.inc will be built in the pre-build phase of the cee_wks build
    TARGET cee_wks PRE_BUILD 
    COMMAND ${POWERSHELL} -NoProfile -ExecutionPolicy Bypass \"& \"\"${VM_DIR}/h2inc.ps1\"\"\" \"\"\"${VM_DIR}/${ARCH_SOURCES_DIR}/asmconstants.h\"\"\" >"${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.tmp"
    COMMAND ${CMAKE_CXX_COMPILER} ${DEFINITIONS} /EP "${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.tmp" >"${CMAKE_CURRENT_BINARY_DIR}/AsmConstants.inc"
)

endif (WIN32)
