
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

      if (CLR_CMAKE_TARGET_ARCH_ARM AND CLR_CMAKE_TARGET_WIN32)
        list(APPEND INC_DIRECTORIES /I${dir})
      else()
        list(APPEND INC_DIRECTORIES -I${dir})
      endif(CLR_CMAKE_TARGET_ARCH_ARM AND CLR_CMAKE_TARGET_WIN32)

    endforeach()
    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories)

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

# Build a list of include directories for consumption by the assembler
function(get_include_directories_asm IncludeDirectories)
    get_directory_property(dirs INCLUDE_DIRECTORIES)

    if (CLR_CMAKE_TARGET_ARCH_ARM AND CLR_CMAKE_TARGET_WIN32)
        list(APPEND INC_DIRECTORIES "-I ")
    endif()

    foreach(dir IN LISTS dirs)
      if (CLR_CMAKE_TARGET_ARCH_ARM AND CLR_CMAKE_TARGET_WIN32)
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
