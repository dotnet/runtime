function(clr_unknown_arch)
    if (WIN32)
        message(FATAL_ERROR "Only AMD64, ARM64, ARM and I386 are supported. Found: ${CMAKE_SYSTEM_PROCESSOR}")
    elseif(CLR_CROSS_COMPONENTS_BUILD)
        message(FATAL_ERROR "Only AMD64, I386 host are supported for linux cross-architecture component. Found: ${CMAKE_SYSTEM_PROCESSOR}")
    else()
        message(FATAL_ERROR "Only AMD64, ARM64 and ARM are supported. Found: ${CMAKE_SYSTEM_PROCESSOR}")
    endif()
endfunction()

# Build a list of compiler definitions by putting -D in front of each define.
function(get_compile_definitions DefinitionName)
    # Get the current list of definitions
    get_directory_property(COMPILE_DEFINITIONS_LIST COMPILE_DEFINITIONS)

    # The entries that contain generator expressions must have the -D inside of the
    # expression. So we transform e.g. $<$<CONFIG:Debug>:_DEBUG> to $<$<CONFIG:Debug>:-D_DEBUG>

    # CMake's support for multiple values within a single generator expression is somewhat ad-hoc.
    # Since we have a number of complex generator expressions, we use them with multiple values to ensure that
    # we don't forget to update all of the generator expressions if one needs to be updated.
    # As a result, we need to expand out the multi-valued generator expression to wrap each individual value here.
    # Otherwise, CMake will fail to expand it.
    set(LastGeneratorExpression "")
    foreach(DEFINITION IN LISTS COMPILE_DEFINITIONS_LIST)
      # If there is a definition that uses the $<TARGET_PROPERTY:prop> generator expression
      # we need to remove it since that generator expression is only valid on binary targets.
      # Assume that the value is 0.
      string(REGEX REPLACE "\\$<TARGET_PROPERTY:[^,>]+>" "0" DEFINITION "${DEFINITION}")

      if (${DEFINITION} MATCHES "^\\$<(.+):([^>]+)(>?)$")
        if("${CMAKE_MATCH_3}" STREQUAL "")
          set(DEFINITION "$<${CMAKE_MATCH_1}:-D${CMAKE_MATCH_2}>")
          set(LastGeneratorExpression "${CMAKE_MATCH_1}")
        else()
          set(DEFINITION "$<${CMAKE_MATCH_1}:-D${CMAKE_MATCH_2}>")
        endif()
      elseif(${DEFINITION} MATCHES "([^>]+)>$")
        # This entry is the last in a list nested within a generator expression.
        set(DEFINITION "$<${LastGeneratorExpression}:-D${CMAKE_MATCH_1}>")
        set(LastGeneratorExpression "")
      elseif(NOT "${LastGeneratorExpression}" STREQUAL "")
        set(DEFINITION "$<${LastGeneratorExpression}:-D${DEFINITION}>")
      else()
        set(DEFINITION -D${DEFINITION})
      endif()
      list(APPEND DEFINITIONS ${DEFINITION})
    endforeach()
    set(${DefinitionName} ${DEFINITIONS} PARENT_SCOPE)
endfunction(get_compile_definitions)

# Build a list of include directories
function(get_include_directories IncludeDirectories)
    get_directory_property(dirs INCLUDE_DIRECTORIES)
    foreach(dir IN LISTS dirs)

      if (CLR_CMAKE_HOST_ARCH_ARM AND WIN32)
        list(APPEND INC_DIRECTORIES /I${dir})
      else()
        list(APPEND INC_DIRECTORIES -I${dir})
      endif(CLR_CMAKE_HOST_ARCH_ARM AND WIN32)

    endforeach()
    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories)

# Build a list of include directories for consumption by the assembler
function(get_include_directories_asm IncludeDirectories)
    get_directory_property(dirs INCLUDE_DIRECTORIES)

    foreach(dir IN LISTS dirs)
      list(APPEND INC_DIRECTORIES -I${dir};)
    endforeach()

    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories_asm)

# Finds and returns unwind libs
function(find_unwind_libs UnwindLibs)
    if(CLR_CMAKE_HOST_ARCH_ARM)
      find_library(UNWIND_ARCH NAMES unwind-arm)
    endif()

    if(CLR_CMAKE_HOST_ARCH_ARM64)
      find_library(UNWIND_ARCH NAMES unwind-aarch64)
    endif()

    if(CLR_CMAKE_HOST_ARCH_AMD64)
      find_library(UNWIND_ARCH NAMES unwind-x86_64)
    endif()

    if(NOT UNWIND_ARCH STREQUAL UNWIND_ARCH-NOTFOUND)
       set(UNWIND_LIBS ${UNWIND_ARCH})
    endif()

    find_library(UNWIND_GENERIC NAMES unwind-generic)

    if(NOT UNWIND_GENERIC STREQUAL UNWIND_GENERIC-NOTFOUND)
      set(UNWIND_LIBS ${UNWIND_LIBS} ${UNWIND_GENERIC})
    endif()

    find_library(UNWIND NAMES unwind)

    if(UNWIND STREQUAL UNWIND-NOTFOUND)
      message(FATAL_ERROR "Cannot find libunwind. Try installing libunwind8-dev or libunwind-devel.")
    endif()

    set(${UnwindLibs} ${UNWIND_LIBS} ${UNWIND} PARENT_SCOPE)
endfunction(find_unwind_libs)

# Set the passed in RetSources variable to the list of sources with added current source directory
# to form absolute paths.
# The parameters after the RetSources are the input files.
function(convert_to_absolute_path RetSources)
    set(Sources ${ARGN})
    foreach(Source IN LISTS Sources)
        list(APPEND AbsolutePathSources ${CMAKE_CURRENT_SOURCE_DIR}/${Source})
    endforeach()
    set(${RetSources} ${AbsolutePathSources} PARENT_SCOPE)
endfunction(convert_to_absolute_path)

#Preprocess file
function(preprocess_file inputFilename outputFilename)
  get_compile_definitions(PREPROCESS_DEFINITIONS)
  get_include_directories(PREPROCESS_INCLUDE_DIRECTORIES)
  if (MSVC)
    add_custom_command(
        OUTPUT ${outputFilename}
        COMMAND ${CMAKE_CXX_COMPILER} ${PREPROCESS_INCLUDE_DIRECTORIES} /P /EP /TC ${PREPROCESS_DEFINITIONS}  /Fi${outputFilename}  ${inputFilename} /nologo
        DEPENDS ${inputFilename}
        COMMENT "Preprocessing ${inputFilename}. Outputting to ${outputFilename}"
    )
  else()
    add_custom_command(
        OUTPUT ${outputFilename}
        COMMAND ${CMAKE_CXX_COMPILER} -E -P ${PREPROCESS_DEFINITIONS} ${PREPROCESS_INCLUDE_DIRECTORIES} -o ${outputFilename} -x c ${inputFilename}
        DEPENDS ${inputFilename}
        COMMENT "Preprocessing ${inputFilename}. Outputting to ${outputFilename}"
    )
  endif()

  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

# preprocess_files(PreprocessedFilesList [fileToPreprocess1 [fileToPreprocess2 ...]])
function(preprocess_files PreprocessedFilesList)
  set(FilesToPreprocess ${ARGN})
  foreach(ASM_FILE IN LISTS FilesToPreprocess)
    # Inserts a custom command in CMake build to preprocess each asm source file
    get_filename_component(name ${ASM_FILE} NAME_WE)
    file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/${name}.asm" ASM_PREPROCESSED_FILE)
    preprocess_file(${ASM_FILE} ${ASM_PREPROCESSED_FILE})
    list(APPEND PreprocessedFiles ${ASM_PREPROCESSED_FILE})
  endforeach()
  set(${PreprocessedFilesList} ${PreprocessedFiles} PARENT_SCOPE)
endfunction()

function(set_exports_linker_option exports_filename)
    if(LD_GNU OR LD_SOLARIS)
        # Add linker exports file option
        if(LD_SOLARIS)
            set(EXPORTS_LINKER_OPTION -Wl,-M,${exports_filename} PARENT_SCOPE)
        else()
            set(EXPORTS_LINKER_OPTION -Wl,--version-script=${exports_filename} PARENT_SCOPE)
        endif()
    elseif(LD_OSX)
        # Add linker exports file option
        set(EXPORTS_LINKER_OPTION -Wl,-exported_symbols_list,${exports_filename} PARENT_SCOPE)
    endif()
endfunction()

# compile_asm(TARGET target ASM_FILES file1 [file2 ...] OUTPUT_OBJECTS [variableName])
# CMake does not support the ARM or ARM64 assemblers on Windows when using the
# MSBuild generator. When the MSBuild generator is in use, we manually compile the assembly files
# using this function.
function(compile_asm)
  set(options "")
  set(oneValueArgs TARGET OUTPUT_OBJECTS)
  set(multiValueArgs ASM_FILES)
  cmake_parse_arguments(COMPILE_ASM "${options}" "${oneValueArgs}" "${multiValueArgs}" ${ARGV})

  get_include_directories_asm(ASM_INCLUDE_DIRECTORIES)

  set (ASSEMBLED_OBJECTS "")

  foreach(ASM_FILE ${COMPILE_ASM_ASM_FILES})
    get_filename_component(name ${ASM_FILE} NAME_WE)
    # Produce object file where CMake would store .obj files for an OBJECT library.
    # ex: artifacts\obj\coreclr\windows.arm64.Debug\src\vm\wks\cee_wks.dir\Debug\AsmHelpers.obj
    set (OBJ_FILE "${CMAKE_CURRENT_BINARY_DIR}/${COMPILE_ASM_TARGET}.dir/${CMAKE_CFG_INTDIR}/${name}.obj")

    # Need to compile asm file using custom command as include directories are not provided to asm compiler
    add_custom_command(OUTPUT ${OBJ_FILE}
                        COMMAND "${CMAKE_ASM_COMPILER}" -g ${ASM_INCLUDE_DIRECTORIES} -o ${OBJ_FILE} ${ASM_FILE}
                        DEPENDS ${ASM_FILE}
                        COMMENT "Assembling ${ASM_FILE} ---> \"${CMAKE_ASM_COMPILER}\" -g ${ASM_INCLUDE_DIRECTORIES} -o ${OBJ_FILE} ${ASM_FILE}")

    # mark obj as source that does not require compile
    set_source_files_properties(${OBJ_FILE} PROPERTIES EXTERNAL_OBJECT TRUE)

    # Add the generated OBJ in the dependency list so that it gets consumed during linkage
    list(APPEND ASSEMBLED_OBJECTS ${OBJ_FILE})
  endforeach()

  set(${COMPILE_ASM_OUTPUT_OBJECTS} ${ASSEMBLED_OBJECTS} PARENT_SCOPE)
endfunction()

function(generate_exports_file)
  set(INPUT_LIST ${ARGN})
  list(GET INPUT_LIST -1 outputFilename)
  list(REMOVE_AT INPUT_LIST -1)

  if(CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
    set(SCRIPT_NAME generateexportedsymbols.sh)
  else()
    set(SCRIPT_NAME generateversionscript.sh)
  endif()

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${CLR_ENG_NATIVE_DIR}/${SCRIPT_NAME} ${INPUT_LIST} >${outputFilename}
    DEPENDS ${INPUT_LIST} ${CLR_ENG_NATIVE_DIR}/${SCRIPT_NAME}
    COMMENT "Generating exports file ${outputFilename}"
  )
  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

function(generate_exports_file_prefix inputFilename outputFilename prefix)

  if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(SCRIPT_NAME generateexportedsymbols.sh)
  else()
    set(SCRIPT_NAME generateversionscript.sh)
    if (NOT ${prefix} STREQUAL "")
        set(EXTRA_ARGS ${prefix})
    endif()
  endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${CLR_ENG_NATIVE_DIR}/${SCRIPT_NAME} ${inputFilename} ${EXTRA_ARGS} >${outputFilename}
    DEPENDS ${inputFilename} ${CLR_ENG_NATIVE_DIR}/${SCRIPT_NAME}
    COMMENT "Generating exports file ${outputFilename}"
  )
  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

function(strip_symbols targetName outputFilename)
  if (CLR_CMAKE_HOST_UNIX)
    set(strip_source_file $<TARGET_FILE:${targetName}>)

    if (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
      set(strip_destination_file ${strip_source_file}.dwarf)

      # Ensure that dsymutil and strip are present
      find_program(DSYMUTIL dsymutil)
      if (DSYMUTIL STREQUAL "DSYMUTIL-NOTFOUND")
        message(FATAL_ERROR "dsymutil not found")
      endif()

      find_program(STRIP strip)
      if (STRIP STREQUAL "STRIP-NOTFOUND")
        message(FATAL_ERROR "strip not found")
      endif()

      string(TOLOWER "${CMAKE_BUILD_TYPE}" LOWERCASE_CMAKE_BUILD_TYPE)
      if (LOWERCASE_CMAKE_BUILD_TYPE STREQUAL release)
        set(strip_command ${STRIP} -no_code_signature_warning -S ${strip_source_file} && codesign -f -s - ${strip_source_file})
      else ()
        set(strip_command)
      endif ()

      add_custom_command(
        TARGET ${targetName}
        POST_BUILD
        VERBATIM
        COMMAND ${DSYMUTIL} --flat --minimize ${strip_source_file}
        COMMAND ${strip_command}
        COMMENT "Stripping symbols from ${strip_source_file} into file ${strip_destination_file}"
        )
    else (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)
      set(strip_destination_file ${strip_source_file}.dbg)

      add_custom_command(
        TARGET ${targetName}
        POST_BUILD
        VERBATIM
        COMMAND ${CMAKE_OBJCOPY} --only-keep-debug ${strip_source_file} ${strip_destination_file}
        COMMAND ${CMAKE_OBJCOPY} --strip-unneeded ${strip_source_file}
        COMMAND ${CMAKE_OBJCOPY} --add-gnu-debuglink=${strip_destination_file} ${strip_source_file}
        COMMENT "Stripping symbols from ${strip_source_file} into file ${strip_destination_file}"
        )
    endif (CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS)

    set(${outputFilename} ${strip_destination_file} PARENT_SCOPE)
  else(CLR_CMAKE_HOST_UNIX)
    get_property(is_multi_config GLOBAL PROPERTY GENERATOR_IS_MULTI_CONFIG)
    if(is_multi_config)
      # We can't use the $<TARGET_PDB_FILE> generator expression here since
      # the generator expression isn't supported on resource DLLs.
      set(${outputFilename} ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb PARENT_SCOPE)
    else()
      # We can't use the $<TARGET_PDB_FILE> generator expression here since
      # the generator expression isn't supported on resource DLLs.
      set(${outputFilename} ${CMAKE_CURRENT_BINARY_DIR}/${targetName}.pdb PARENT_SCOPE)
    endif()
  endif(CLR_CMAKE_HOST_UNIX)
endfunction()

function(install_with_stripped_symbols targetName kind destination)
    if(NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS)
      strip_symbols(${targetName} symbol_file)
      install_symbols(${symbol_file} ${destination})
    endif()

    if ((CLR_CMAKE_TARGET_OSX OR CLR_CMAKE_TARGET_MACCATALYST OR CLR_CMAKE_TARGET_IOS OR CLR_CMAKE_TARGET_TVOS) AND ("${kind}" STREQUAL "TARGETS"))
      # We want to avoid the kind=TARGET install behaviors which corrupt code signatures on osx-arm64
      set(kind PROGRAMS)
    endif()

    if ("${kind}" STREQUAL "TARGETS")
      set(install_source ${targetName})
    elseif("${kind}" STREQUAL "PROGRAMS")
      set(install_source $<TARGET_FILE:${targetName}>)
    else()
      message(FATAL_ERROR "The `kind` argument has to be either TARGETS or PROGRAMS, ${kind} was provided instead")
    endif()
    install(${kind} ${install_source} DESTINATION ${destination})
endfunction()

function(install_symbols symbol_file destination_path)
  if(CLR_CMAKE_TARGET_WIN32)
    install(FILES ${symbol_file} DESTINATION ${destination_path}/PDB)
  else()
    install(FILES ${symbol_file} DESTINATION ${destination_path})
  endif()
endfunction()

# install_clr(TARGETS TARGETS targetName [targetName2 ...] [ADDITIONAL_DESTINATIONS destination])
function(install_clr)
  set(multiValueArgs TARGETS ADDITIONAL_DESTINATIONS)
  cmake_parse_arguments(INSTALL_CLR "" "" "${multiValueArgs}" ${ARGV})

  if ("${INSTALL_CLR_TARGETS}" STREQUAL "")
    message(FATAL_ERROR "At least one target must be passed to install_clr(TARGETS )")
  endif()

  set(destinations ".")

  if (NOT "${INSTALL_CLR_ADDITIONAL_DESTINATIONS}" STREQUAL "")
    list(APPEND destinations ${INSTALL_CLR_ADDITIONAL_DESTINATIONS})
  endif()

  foreach(targetName ${INSTALL_CLR_TARGETS})
    list(FIND CLR_CROSS_COMPONENTS_LIST ${targetName} INDEX)
    if (NOT DEFINED CLR_CROSS_COMPONENTS_LIST OR NOT ${INDEX} EQUAL -1)
        if (NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS)
          strip_symbols(${targetName} symbol_file)
        endif()

        foreach(destination ${destinations})
          # We don't need to install the export libraries for our DLLs
          # since they won't be directly linked against.
          install(PROGRAMS $<TARGET_FILE:${targetName}> DESTINATION ${destination})
          if (NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS)
            install_symbols(${symbol_file} ${destination})
          endif()

          if(CLR_CMAKE_PGO_INSTRUMENT)
            if(WIN32)
              get_property(is_multi_config GLOBAL PROPERTY GENERATOR_IS_MULTI_CONFIG)
              if(is_multi_config)
                  install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pgd DESTINATION ${destination}/PGD OPTIONAL)
              else()
                  install(FILES ${CMAKE_CURRENT_BINARY_DIR}/${targetName}.pgd DESTINATION ${destination}/PGD OPTIONAL)
              endif()
            endif()
          endif()
        endforeach()
    endif()
  endforeach()
endfunction()

# Disable PAX mprotect that would prevent JIT and other codegen in coreclr from working.
# PAX mprotect prevents:
# - changing the executable status of memory pages that were
#   not originally created as executable,
# - making read-only executable pages writable again,
# - creating executable pages from anonymous memory,
# - making read-only-after-relocations (RELRO) data pages writable again.
function(disable_pax_mprotect targetName)
  # Try to locate the paxctl tool. Failure to find it is not fatal,
  # but the generated executables won't work on a system where PAX is set
  # to prevent applications to create executable memory mappings.
  find_program(PAXCTL paxctl)

  if (NOT PAXCTL STREQUAL "PAXCTL-NOTFOUND")
    add_custom_command(
      TARGET ${targetName}
      POST_BUILD
      VERBATIM
      COMMAND ${PAXCTL} -c -m $<TARGET_FILE:${targetName}>
    )
  endif()
endfunction()

if (CMAKE_VERSION VERSION_LESS "3.12")
  # Polyfill add_compile_definitions when it is unavailable
  function(add_compile_definitions)
    get_directory_property(DIR_COMPILE_DEFINITIONS COMPILE_DEFINITIONS)
    list(APPEND DIR_COMPILE_DEFINITIONS ${ARGV})
    set_directory_properties(PROPERTIES COMPILE_DEFINITIONS "${DIR_COMPILE_DEFINITIONS}")
  endfunction()
endif()

if (CMAKE_VERSION VERSION_LESS "3.16")
  # Provide a no-op polyfill for precompiled headers on old CMake versions
  function(target_precompile_headers)
  endfunction()
endif()

function(_add_executable)
    if(NOT WIN32)
      add_executable(${ARGV} ${VERSION_FILE_PATH})
      disable_pax_mprotect(${ARGV})
    else()
      add_executable(${ARGV})
    endif(NOT WIN32)
    list(FIND CLR_CROSS_COMPONENTS_LIST ${ARGV0} INDEX)
    if (DEFINED CLR_CROSS_COMPONENTS_LIST AND ${INDEX} EQUAL -1)
     set_target_properties(${ARGV0} PROPERTIES EXCLUDE_FROM_ALL 1)
    endif()
endfunction()

function(_add_library)
    if(NOT WIN32 AND "${ARGV1}" STREQUAL "SHARED")
      add_library(${ARGV} ${VERSION_FILE_PATH})
    else()
      add_library(${ARGV})
    endif(NOT WIN32 AND "${ARGV1}" STREQUAL "SHARED")
    list(FIND CLR_CROSS_COMPONENTS_LIST ${ARGV0} INDEX)
    if (DEFINED CLR_CROSS_COMPONENTS_LIST AND ${INDEX} EQUAL -1)
     set_target_properties(${ARGV0} PROPERTIES EXCLUDE_FROM_ALL 1)
    endif()
endfunction()

function(_install)
    if(NOT DEFINED CLR_CROSS_COMPONENTS_BUILD)
      install(${ARGV})
    endif()
endfunction()

function(add_library_clr)
    _add_library(${ARGV})
endfunction()

function(add_executable_clr)
    _add_executable(${ARGV})
endfunction()

function(generate_module_index Target ModuleIndexFile)
    if(CLR_CMAKE_HOST_WIN32)
        set(scriptExt ".cmd")
    else()
        set(scriptExt ".sh")
    endif()

    add_custom_command(
        OUTPUT ${ModuleIndexFile}
        COMMAND ${CLR_ENG_NATIVE_DIR}/genmoduleindex${scriptExt} $<TARGET_FILE:${Target}> ${ModuleIndexFile}
        DEPENDS ${Target}
        COMMENT "Generating ${Target} module index file -> ${ModuleIndexFile}"
    )

    set_source_files_properties(
        ${ModuleIndexFile}
        PROPERTIES GENERATED TRUE
    )

    add_custom_target(
        ${Target}_module_index_header
        DEPENDS ${ModuleIndexFile}
    )
endfunction(generate_module_index)

# add_linker_flag(Flag [Config1 Config2 ...])
function(add_linker_flag Flag)
  if (ARGN STREQUAL "")
    set("CMAKE_EXE_LINKER_FLAGS" "${CMAKE_EXE_LINKER_FLAGS} ${Flag}" PARENT_SCOPE)
    set("CMAKE_SHARED_LINKER_FLAGS" "${CMAKE_SHARED_LINKER_FLAGS} ${Flag}" PARENT_SCOPE)
  else()
    foreach(Config ${ARGN})
      set("CMAKE_EXE_LINKER_FLAGS_${Config}" "${CMAKE_EXE_LINKER_FLAGS_${Config}} ${Flag}" PARENT_SCOPE)
      set("CMAKE_SHARED_LINKER_FLAGS_${Config}" "${CMAKE_SHARED_LINKER_FLAGS_${Config}} ${Flag}" PARENT_SCOPE)
    endforeach()
  endif()
endfunction()
