function(clr_unknown_arch)
    if (WIN32)
        message(FATAL_ERROR "Only AMD64, ARM64, ARM and I386 are supported")
    elseif(CLR_CROSS_COMPONENTS_BUILD)
        message(FATAL_ERROR "Only AMD64, I386 host are supported for linux cross-architecture component")
    else()
        message(FATAL_ERROR "Only AMD64, ARM64 and ARM are supported")
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

      if (CLR_CMAKE_PLATFORM_ARCH_ARM AND WIN32)
        list(APPEND INC_DIRECTORIES /I${dir})
      else()
        list(APPEND INC_DIRECTORIES -I${dir})
      endif(CLR_CMAKE_PLATFORM_ARCH_ARM AND WIN32)

    endforeach()
    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories)

# Build a list of include directories for consumption by the assembler
function(get_include_directories_asm IncludeDirectories)
    get_directory_property(dirs INCLUDE_DIRECTORIES)

    if (CLR_CMAKE_PLATFORM_ARCH_ARM AND WIN32)
        list(APPEND INC_DIRECTORIES "-I ")
    endif()

    foreach(dir IN LISTS dirs)
      if (CLR_CMAKE_PLATFORM_ARCH_ARM AND WIN32)
        list(APPEND INC_DIRECTORIES ${dir};)
      else()
        list(APPEND INC_DIRECTORIES -I${dir})
      endif()
    endforeach()

    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories_asm)

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
        COMMAND ${CMAKE_CXX_COMPILER} ${PREPROCESS_INCLUDE_DIRECTORIES} /P /EP /TC ${PREPROCESS_DEFINITIONS}  /Fi${outputFilename}  ${inputFilename}
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

# preprocess_compile_asm(ASM_FILES file1 [file2 ...] OUTPUT_OBJECTS [variableName])
function(preprocess_compile_asm)
  set(options "")
  set(oneValueArgs OUTPUT_OBJECTS)
  set(multiValueArgs ASM_FILES)
  cmake_parse_arguments(PARSE_ARGV 0 COMPILE_ASM "${options}" "${oneValueArgs}" "${multiValueArgs}")
  
  get_include_directories_asm(ASM_INCLUDE_DIRECTORIES)

  set (ASSEMBLED_OBJECTS "")

  foreach(ASM_FILE ${COMPILE_ASM_ASM_FILES})
    # Inserts a custom command in CMake build to preprocess each asm source file
    get_filename_component(name ${ASM_FILE} NAME_WE)
    file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/${name}.asm" ASM_PREPROCESSED_FILE)
    preprocess_file(${ASM_FILE} ${ASM_PREPROCESSED_FILE})

    # We do not pass any defines since we have already done pre-processing above
    set (ASM_CMDLINE "-o ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj ${ASM_PREPROCESSED_FILE}")

    # Generate the batch file that will invoke the assembler
    file(TO_CMAKE_PATH "${CMAKE_CURRENT_BINARY_DIR}/runasm_${name}.cmd" ASM_SCRIPT_FILE)

    file(GENERATE OUTPUT "${ASM_SCRIPT_FILE}"
        CONTENT "\"${CMAKE_ASM_MASM_COMPILER}\" -g ${ASM_INCLUDE_DIRECTORIES} ${ASM_CMDLINE}")

    message("Generated  - ${ASM_SCRIPT_FILE}")

    # Need to compile asm file using custom command as include directories are not provided to asm compiler
    add_custom_command(OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj
                        COMMAND ${ASM_SCRIPT_FILE}
                        DEPENDS ${ASM_PREPROCESSED_FILE}
                        COMMENT "Assembling ${ASM_PREPROCESSED_FILE} - ${ASM_SCRIPT_FILE}")

    # mark obj as source that does not require compile
    set_source_files_properties(${CMAKE_CURRENT_BINARY_DIR}/${name}.obj PROPERTIES EXTERNAL_OBJECT TRUE)

    # Add the generated OBJ in the dependency list so that it gets consumed during linkage
    list(APPEND ASSEMBLED_OBJECTS ${CMAKE_CURRENT_BINARY_DIR}/${name}.obj)
  endforeach()

  set(${COMPILE_ASM_OUTPUT_OBJECTS} ${ASSEMBLED_OBJECTS} PARENT_SCOPE)
endfunction()

function(generate_exports_file)
  set(INPUT_LIST ${ARGN})
  list(GET INPUT_LIST -1 outputFilename)
  list(REMOVE_AT INPUT_LIST -1)

  if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(AWK_SCRIPT generateexportedsymbols.awk)
  else()
    set(AWK_SCRIPT generateversionscript.awk)
  endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${AWK} -f ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT} ${INPUT_LIST} >${outputFilename}
    DEPENDS ${INPUT_LIST} ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT}
    COMMENT "Generating exports file ${outputFilename}"
  )
  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

function(generate_exports_file_prefix inputFilename outputFilename prefix)

  if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(AWK_SCRIPT generateexportedsymbols.awk)
  else()
    set(AWK_SCRIPT generateversionscript.awk)
    if (NOT ${prefix} STREQUAL "")
        set(AWK_VARS ${AWK_VARS} -v prefix=${prefix})
    endif()
  endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${AWK} -f ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT} ${AWK_VARS} ${inputFilename} >${outputFilename}
    DEPENDS ${inputFilename} ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT}
    COMMENT "Generating exports file ${outputFilename}"
  )
  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

# target_precompile_header(TARGET targetName HEADER headerName [ADDITIONAL_INCLUDE_DIRECTORIES includeDirs])
function(target_precompile_header)
  set(options "")
  set(oneValueArgs TARGET HEADER)
  set(multiValueArgs ADDITIONAL_INCLUDE_DIRECTORIES)
  cmake_parse_arguments(PARSE_ARGV 0 PRECOMPILE_HEADERS "${options}" "${oneValueArgs}" "${multiValueArgs}")

  if ("${PRECOMPILE_HEADERS_TARGET}" STREQUAL "")
  message(SEND_ERROR "No target supplied to target_precompile_header.")
  endif()
  if ("${PRECOMPILE_HEADERS_HEADER}" STREQUAL "")
    message(SEND_ERROR "No header supplied to target_precompile_header.")
  endif()

  if(MSVC)
    get_filename_component(PCH_NAME ${PRECOMPILE_HEADERS_HEADER} NAME_WE)
    # We need to use the $<TARGET_PROPERTY:NAME> generator here instead of the ${targetName} variable since
    # CMake evaluates source file properties once per directory. If we just use ${targetName}, we end up sharing
    # the same PCH between targets, which doesn't work.
    set(precompiledBinary "${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/${PCH_NAME}.$<TARGET_PROPERTY:NAME>.pch")
    set(pchSourceFile "${CMAKE_CURRENT_BINARY_DIR}/${PCH_NAME}.${PRECOMPILE_HEADERS_TARGET}.cpp")

    file(GENERATE OUTPUT ${pchSourceFile} CONTENT "#include \"${PRECOMPILE_HEADERS_HEADER}\"")

    set(PCH_SOURCE_FILE_INCLUDE_DIRECTORIES ${CMAKE_CURRENT_SOURCE_DIR} ${PRECOMPILE_HEADERS_ADDITIONAL_INCLUDE_DIRECTORIES})

    set_source_files_properties(${pchSourceFile}
                                PROPERTIES COMPILE_FLAGS "/Yc\"${PRECOMPILE_HEADERS_HEADER}\" /Fp\"${precompiledBinary}\""
                                            OBJECT_OUTPUTS "${precompiledBinary}"
                                            INCLUDE_DIRECTORIES "${PCH_SOURCE_FILE_INCLUDE_DIRECTORIES}")
    get_target_property(TARGET_SOURCES ${PRECOMPILE_HEADERS_TARGET} SOURCES)

    foreach (SOURCE ${TARGET_SOURCES})
      get_source_file_property(SOURCE_LANG ${SOURCE} LANGUAGE)
      if (("${SOURCE_LANG}" STREQUAL "C") OR ("${SOURCE_LANG}" STREQUAL "CXX"))
        set_source_files_properties(${SOURCE}
          PROPERTIES COMPILE_FLAGS "/Yu\"${PRECOMPILE_HEADERS_HEADER}\" /Fp\"${precompiledBinary}\""
                      OBJECT_DEPENDS "${precompiledBinary}")
      endif()
    endforeach()

    # Add pchSourceFile to PRECOMPILE_HEADERS_TARGET target
    target_sources(${PRECOMPILE_HEADERS_TARGET} PRIVATE ${pchSourceFile})
  endif(MSVC)
endfunction()

function(strip_symbols targetName outputFilename)
  if (CLR_CMAKE_PLATFORM_UNIX)
    if (STRIP_SYMBOLS)
      set(strip_source_file $<TARGET_FILE:${targetName}>)

      if (CMAKE_SYSTEM_NAME STREQUAL Darwin)
        set(strip_destination_file ${strip_source_file}.dwarf)

        add_custom_command(
          TARGET ${targetName}
          POST_BUILD
          VERBATIM
          COMMAND ${DSYMUTIL} --flat --minimize ${strip_source_file}
          COMMAND ${STRIP} -S ${strip_source_file}
          COMMENT Stripping symbols from ${strip_source_file} into file ${strip_destination_file}
        )
      else (CMAKE_SYSTEM_NAME STREQUAL Darwin)
        set(strip_destination_file ${strip_source_file}.dbg)

        add_custom_command(
          TARGET ${targetName}
          POST_BUILD
          VERBATIM
          COMMAND ${OBJCOPY} --only-keep-debug ${strip_source_file} ${strip_destination_file}
          COMMAND ${OBJCOPY} --strip-debug ${strip_source_file}
          COMMAND ${OBJCOPY} --add-gnu-debuglink=${strip_destination_file} ${strip_source_file}
          COMMENT Stripping symbols from ${strip_source_file} into file ${strip_destination_file}
        )
      endif (CMAKE_SYSTEM_NAME STREQUAL Darwin)

      set(${outputFilename} ${strip_destination_file} PARENT_SCOPE)
    endif (STRIP_SYMBOLS)
  endif(CLR_CMAKE_PLATFORM_UNIX)
endfunction()

function(install_clr targetName)
  list(FIND CLR_CROSS_COMPONENTS_LIST ${targetName} INDEX)
  if (NOT DEFINED CLR_CROSS_COMPONENTS_LIST OR NOT ${INDEX} EQUAL -1)
    strip_symbols(${targetName} strip_destination_file)

    # We don't need to install the export libraries for our DLLs
    # since they won't be directly linked against.
    install(PROGRAMS $<TARGET_FILE:${targetName}> DESTINATION .)
    if(WIN32)
        # We can't use the $<TARGET_PDB_FILE> generator expression here since
        # the generator expression isn't supported on resource DLLs.
        install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION PDB)
    else()
        install(FILES ${strip_destination_file} DESTINATION .)
    endif()
    if(CLR_CMAKE_PGO_INSTRUMENT)
        if(WIN32)
            install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pgd DESTINATION PGD OPTIONAL)
        endif()
    endif()
  endif()
endfunction()

# Disable PAX mprotect that would prevent JIT and other codegen in coreclr from working.
# PAX mprotect prevents:
# - changing the executable status of memory pages that were
#   not originally created as executable,
# - making read-only executable pages writable again,
# - creating executable pages from anonymous memory,
# - making read-only-after-relocations (RELRO) data pages writable again.
function(disable_pax_mprotect targetName)
  if (NOT PAXCTL STREQUAL "PAXCTL-NOTFOUND")
    add_custom_command(
      TARGET ${targetName}
      POST_BUILD
      VERBATIM
      COMMAND ${PAXCTL} -c -m $<TARGET_FILE:${targetName}>
    )
  endif()
endfunction()

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
    if(NOT WIN32)
      add_library(${ARGV} ${VERSION_FILE_PATH})
    else()
      add_library(${ARGV})
    endif(NOT WIN32)
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

function(verify_dependencies targetName errorMessage)
    set(SANITIZER_BUILD OFF)

    if (CLR_CMAKE_PLATFORM_UNIX)
        if (UPPERCASE_CMAKE_BUILD_TYPE STREQUAL DEBUG OR UPPERCASE_CMAKE_BUILD_TYPE STREQUAL CHECKED)
            string(FIND "$ENV{DEBUG_SANITIZERS}" "asan" __ASAN_POS)
            string(FIND "$ENV{DEBUG_SANITIZERS}" "ubsan" __UBSAN_POS)
            if ((${__ASAN_POS} GREATER -1) OR (${__UBSAN_POS} GREATER -1))
                set(SANITIZER_BUILD ON)
            endif()
        endif()
    endif()

    # We don't need to verify dependencies on OSX, since missing dependencies
    # result in link error over there.
    # Also don't verify dependencies for Asan build because in this case shared
    # libraries can contain undefined symbols
    if (NOT CLR_CMAKE_PLATFORM_DARWIN AND NOT CLR_CMAKE_PLATFORM_ANDROID AND NOT SANITIZER_BUILD)
        add_custom_command(
            TARGET ${targetName}
            POST_BUILD
            VERBATIM
            COMMAND ${CMAKE_SOURCE_DIR}/verify-so.sh
                $<TARGET_FILE:${targetName}>
                ${errorMessage}
            COMMENT "Verifying ${targetName} dependencies"
        )
    endif()
endfunction()

function(add_library_clr)
    _add_library(${ARGV})
endfunction()

function(add_executable_clr)
    _add_executable(${ARGV})
endfunction()
