function(clr_unknown_arch)
    if (WIN32)
        message(FATAL_ERROR "Only AMD64, ARM64, ARM and I386 are supported. Found: ${CMAKE_SYSTEM_PROCESSOR}")
    elseif(CLR_CROSS_COMPONENTS_BUILD)
        message(FATAL_ERROR "Only AMD64, I386 host are supported for linux cross-architecture component. Found: ${CMAKE_SYSTEM_PROCESSOR}")
    else()
        message(FATAL_ERROR "'${CMAKE_SYSTEM_PROCESSOR}' is an unsupported architecture.")
    endif()
endfunction()

# C to MASM include file translator
# This is replacement for the deprecated h2inc tool that used to be part of VS.
function(h2inc filename output)
    file(STRINGS ${filename} lines)
    get_filename_component(path "${filename}" DIRECTORY)
    file(RELATIVE_PATH relative_filename "${CLR_REPO_ROOT_DIR}" "${filename}")

    file(WRITE "${output}" "// File start: ${relative_filename}\n")

    # Use of NEWLINE_CONSUME is needed for lines with trailing backslash
    file(STRINGS ${filename} contents NEWLINE_CONSUME)
    string(REGEX REPLACE "\\\\\n" "\\\\\\\\ \n" contents "${contents}")
    string(REGEX REPLACE "\n" ";" lines "${contents}")

    foreach(line IN LISTS lines)
        string(REGEX REPLACE "\\\\\\\\ " "\\\\" line "${line}")

        if(line MATCHES "^ *# pragma")
            # Ignore pragmas
            continue()
        endif()

        if(line MATCHES "^ *# *include *\"(.*)\"")
            # Expand includes.
            h2inc("${path}/${CMAKE_MATCH_1}" "${output}")
            continue()
        endif()

        if(line MATCHES "^ *#define +([0-9A-Za-z_()]+) *(.*)")
            # Augment #defines with their MASM equivalent
            set(name "${CMAKE_MATCH_1}")
            set(value "${CMAKE_MATCH_2}")

            # Note that we do not handle multiline constants

            # Strip comments from value
            string(REGEX REPLACE "//.*" "" value "${value}")
            string(REGEX REPLACE "/\\*.*\\*/" "" value "${value}")

            # Strip whitespaces from value
            string(REPLACE " +$" "" value "${value}")

            # ignore #defines with arguments
            if(NOT "${name}" MATCHES "\\(")
                set(HEX_NUMBER_PATTERN "0x([0-9A-Fa-f]+)")
                set(DECIMAL_NUMBER_PATTERN "(-?[0-9]+)")

                if("${value}" MATCHES "${HEX_NUMBER_PATTERN}")
                    string(REGEX REPLACE "${HEX_NUMBER_PATTERN}" "0\\1h" value "${value}")    # Convert hex constants
                    file(APPEND "${output}" "${name} EQU ${value}\n")
                elseif("${value}" MATCHES "${DECIMAL_NUMBER_PATTERN}" AND (NOT "${value}" MATCHES "[G-Zg-z]+" OR "${value}" MATCHES "\\("))
                    string(REGEX REPLACE "${DECIMAL_NUMBER_PATTERN}" "\\1t" value "${value}") # Convert dec constants
                    file(APPEND "${output}" "${name} EQU ${value}\n")
                else()
                    file(APPEND "${output}" "${name} TEXTEQU <${value}>\n")
                endif()
            endif()
        endif()

        file(APPEND "${output}" "${line}\n")
    endforeach()

    file(APPEND "${output}" "// File end: ${relative_filename}\n")
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
      # or the $<COMPILE_LANGUAGE:lang> generator expression,
      # we need to remove it since that generator expression is only valid on binary targets.
      # Assume that the value is 0.
      string(REGEX REPLACE "\\$<TARGET_PROPERTY:[^,>]+>" "0" DEFINITION "${DEFINITION}")
      string(REGEX REPLACE "\\$<COMPILE_LANGUAGE:[^>]+(,[^>]+)*>" "0" DEFINITION "${DEFINITION}")

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

# Adds prefix to paths list
function(addprefix var prefix list)
  set(f)
  foreach(i ${list})
    set(f ${f} ${prefix}/${i})
  endforeach()
  set(${var} ${f} PARENT_SCOPE)
endfunction()

# Finds and returns unwind libs
function(find_unwind_libs UnwindLibs)
    if(CLR_CMAKE_HOST_ARCH_ARM)
      find_library(UNWIND_ARCH NAMES unwind-arm)
    endif()

    if(CLR_CMAKE_HOST_ARCH_ARMV6)
      find_library(UNWIND_ARCH NAMES unwind-arm)
    endif()

    if(CLR_CMAKE_HOST_ARCH_ARM64)
      find_library(UNWIND_ARCH NAMES unwind-aarch64)
    endif()

    if(CLR_CMAKE_HOST_ARCH_LOONGARCH64)
      find_library(UNWIND_ARCH NAMES unwind-loongarch64)
    endif()

    if(CLR_CMAKE_HOST_ARCH_RISCV64)
      find_library(UNWIND_ARCH NAMES unwind-riscv64)
    endif()

    if(CLR_CMAKE_HOST_ARCH_AMD64)
      find_library(UNWIND_ARCH NAMES unwind-x86_64)
    endif()

    if(CLR_CMAKE_HOST_ARCH_S390X)
      find_library(UNWIND_ARCH NAMES unwind-s390x)
    endif()

    if(CLR_CMAKE_HOST_ARCH_POWERPC64)
      find_library(UNWIND_ARCH NAMES unwind-ppc64le)
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
      get_filename_component(AbsolutePathSource ${Source} ABSOLUTE BASE_DIR ${CMAKE_CURRENT_SOURCE_DIR})
      list(APPEND AbsolutePathSources ${AbsolutePathSource})
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
    if(LD_GNU OR LD_SOLARIS OR LD_LLVM)
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

# add_component(componentName [targetName] [EXCLUDE_FROM_ALL])
function(add_component componentName)
  if (${ARGC} GREATER 2 OR ${ARGC} EQUAL 2)
    set(componentTargetName "${ARGV1}")
  else()
    set(componentTargetName "${componentName}")
  endif()
  if (${ARGC} EQUAL 3 AND "${ARG2}" STREQUAL "EXCLUDE_FROM_ALL")
    set(exclude_from_all_flag "EXCLUDE_FROM_ALL")
  endif()
  get_property(definedComponents GLOBAL PROPERTY CLR_CMAKE_COMPONENTS)
  list (FIND definedComponents "${componentName}" componentIndex)
  if (${componentIndex} EQUAL -1)
    list (APPEND definedComponents "${componentName}")
    add_custom_target("${componentTargetName}"
      COMMAND "${CMAKE_COMMAND}" "-DCMAKE_INSTALL_COMPONENT=${componentName}" "-DBUILD_TYPE=$<CONFIG>" -P "${CMAKE_BINARY_DIR}/cmake_install.cmake"
      ${exclude_from_all_flag})
    set_property(GLOBAL PROPERTY CLR_CMAKE_COMPONENTS ${definedComponents})
  endif()
endfunction()

function(generate_exports_file)
  set(INPUT_LIST ${ARGN})
  list(GET INPUT_LIST -1 outputFilename)
  list(REMOVE_AT INPUT_LIST -1)

  if(CLR_CMAKE_TARGET_APPLE)
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

function (get_symbol_file_name targetName outputSymbolFilename)
  if (CLR_CMAKE_HOST_UNIX)
    if (CLR_CMAKE_TARGET_APPLE)
      set(strip_destination_file $<TARGET_FILE:${targetName}>.dwarf)
    else ()
      set(strip_destination_file $<TARGET_FILE:${targetName}>.dbg)
    endif ()

    set(${outputSymbolFilename} ${strip_destination_file} PARENT_SCOPE)
  elseif(CLR_CMAKE_HOST_WIN32)
    # We can't use the $<TARGET_PDB_FILE> generator expression here since
    # the generator expression isn't supported on resource DLLs.
    set(${outputSymbolFilename} $<TARGET_FILE_DIR:${targetName}>/$<TARGET_FILE_PREFIX:${targetName}>$<TARGET_FILE_BASE_NAME:${targetName}>.pdb PARENT_SCOPE)
  endif()
endfunction()

function(strip_symbols targetName outputFilename)
  get_symbol_file_name(${targetName} strip_destination_file)
  set(${outputFilename} ${strip_destination_file} PARENT_SCOPE)
  if (CLR_CMAKE_HOST_UNIX)
    set(strip_source_file $<TARGET_FILE:${targetName}>)

    if (CLR_CMAKE_TARGET_APPLE)

      # Ensure that dsymutil and strip are present
      find_program(DSYMUTIL dsymutil)
      if (DSYMUTIL STREQUAL "DSYMUTIL-NOTFOUND")
        message(FATAL_ERROR "dsymutil not found")
      endif()

      find_program(STRIP strip)
      if (STRIP STREQUAL "STRIP-NOTFOUND")
        message(FATAL_ERROR "strip not found")
      endif()

      set(strip_command ${STRIP} -no_code_signature_warning -S ${strip_source_file})

      if (CLR_CMAKE_TARGET_OSX)
        # codesign release build
        string(TOLOWER "${CMAKE_BUILD_TYPE}" LOWERCASE_CMAKE_BUILD_TYPE)
        if (LOWERCASE_CMAKE_BUILD_TYPE STREQUAL release)
          set(strip_command ${strip_command} && codesign -f -s - ${strip_source_file})
        endif ()
      endif ()

      execute_process(
        COMMAND ${DSYMUTIL} --help
        OUTPUT_VARIABLE DSYMUTIL_HELP_OUTPUT
      )

      set(DSYMUTIL_OPTS "--flat")
      if ("${DSYMUTIL_HELP_OUTPUT}" MATCHES "--minimize")
        list(APPEND DSYMUTIL_OPTS "--minimize")
      endif ()

      add_custom_command(
        TARGET ${targetName}
        POST_BUILD
        VERBATIM
        COMMAND sh -c "echo Stripping symbols from $(basename '${strip_source_file}') into $(basename '${strip_destination_file}')"
        COMMAND ${DSYMUTIL} ${DSYMUTIL_OPTS} ${strip_source_file}
        COMMAND ${strip_command}
        )
    else (CLR_CMAKE_TARGET_APPLE)

      add_custom_command(
        TARGET ${targetName}
        POST_BUILD
        VERBATIM
        COMMAND sh -c "echo Stripping symbols from $(basename '${strip_source_file}') into $(basename '${strip_destination_file}')"
        COMMAND ${CMAKE_OBJCOPY} --only-keep-debug ${strip_source_file} ${strip_destination_file}
        COMMAND ${CMAKE_OBJCOPY} --strip-debug --strip-unneeded ${strip_source_file}
        COMMAND ${CMAKE_OBJCOPY} --add-gnu-debuglink=${strip_destination_file} ${strip_source_file}
        )
    endif (CLR_CMAKE_TARGET_APPLE)
  endif(CLR_CMAKE_HOST_UNIX)
endfunction()

function(install_with_stripped_symbols targetName kind destination)
    get_property(target_is_framework TARGET ${targetName} PROPERTY "FRAMEWORK")
    if(NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS)
      strip_symbols(${targetName} symbol_file)
      if (NOT "${symbol_file}" STREQUAL "" AND NOT target_is_framework)
        install_symbol_file(${symbol_file} ${destination} ${ARGN})
      endif()
    endif()

    if (target_is_framework)
      install(TARGETS ${targetName} FRAMEWORK DESTINATION ${destination} ${ARGN})
    else()
      if (CLR_CMAKE_TARGET_APPLE AND ("${kind}" STREQUAL "TARGETS"))
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
      install(${kind} ${install_source} DESTINATION ${destination} ${ARGN})
    endif()
endfunction()

function(install_symbol_file symbol_file destination_path)
  if(CLR_CMAKE_TARGET_WIN32)
      install(FILES ${symbol_file} DESTINATION ${destination_path}/PDB ${ARGN})
  else()
      install(FILES ${symbol_file} DESTINATION ${destination_path} ${ARGN})
  endif()
endfunction()

function(install_static_library targetName destination component)
  if (NOT "${component}" STREQUAL "${targetName}")
    get_property(definedComponents GLOBAL PROPERTY CLR_CMAKE_COMPONENTS)
    list(FIND definedComponents "${component}" componentIdx)
    if (${componentIdx} EQUAL -1)
      message(FATAL_ERROR "The ${component} component is not defined. Add a call to `add_component(${component})` to define the component in the build.")
    endif()
    add_dependencies(${component} ${targetName})
  endif()
  install (TARGETS ${targetName} DESTINATION ${destination} COMPONENT ${component})
  if (WIN32)
    set_target_properties(${targetName} PROPERTIES
        COMPILE_PDB_NAME "${targetName}"
        COMPILE_PDB_OUTPUT_DIRECTORY "${PROJECT_BINARY_DIR}"
    )
    install (FILES "$<TARGET_FILE_DIR:${targetName}>/${targetName}.pdb" DESTINATION ${destination} COMPONENT ${component})
  endif()
endfunction()

# install_clr(TARGETS targetName [targetName2 ...] [DESTINATIONS destination [destination2 ...]] [COMPONENT componentName])
function(install_clr)
  set(multiValueArgs TARGETS DESTINATIONS)
  set(singleValueArgs COMPONENT)
  set(options "")
  cmake_parse_arguments(INSTALL_CLR "${options}" "${singleValueArgs}" "${multiValueArgs}" ${ARGV})

  if ("${INSTALL_CLR_TARGETS}" STREQUAL "")
    message(FATAL_ERROR "At least one target must be passed to install_clr(TARGETS )")
  endif()

  if ("${INSTALL_CLR_DESTINATIONS}" STREQUAL "")
    message(FATAL_ERROR "At least one destination must be passed to install_clr.")
  endif()

  set(destinations "")

  if (NOT "${INSTALL_CLR_DESTINATIONS}" STREQUAL "")
    list(APPEND destinations ${INSTALL_CLR_DESTINATIONS})
  endif()

  if ("${INSTALL_CLR_COMPONENT}" STREQUAL "")
    set(INSTALL_CLR_COMPONENT ${CMAKE_INSTALL_DEFAULT_COMPONENT_NAME})
  endif()

  foreach(targetName ${INSTALL_CLR_TARGETS})
    if (NOT "${INSTALL_CLR_COMPONENT}" STREQUAL "${targetName}")
      get_property(definedComponents GLOBAL PROPERTY CLR_CMAKE_COMPONENTS)
      list(FIND definedComponents "${INSTALL_CLR_COMPONENT}" componentIdx)
      if (${componentIdx} EQUAL -1)
        message(FATAL_ERROR "The ${INSTALL_CLR_COMPONENT} component is not defined. Add a call to `add_component(${INSTALL_CLR_COMPONENT})` to define the component in the build.")
      endif()
      add_dependencies(${INSTALL_CLR_COMPONENT} ${targetName})
    endif()
    get_target_property(targetImportedNativeAotLib ${targetName} CLR_IMPORTED_NATIVEAOT_LIBRARY)
    get_target_property(targetType ${targetName} TYPE)
    if (NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS AND NOT "${targetType}" STREQUAL "STATIC_LIBRARY" AND NOT "${targetImportedNativeAotLib}")
      get_symbol_file_name(${targetName} symbolFile)
    endif()
    # FIXME: make symbol files for native aot libs too

    foreach(destination ${destinations})
      # We don't need to install the export libraries for our DLLs
      # since they won't be directly linked against.
      if (NOT "${targetImportedNativeAotLib}")
        install(PROGRAMS $<TARGET_FILE:${targetName}> DESTINATION ${destination} COMPONENT ${INSTALL_CLR_COMPONENT})
        if (NOT "${symbolFile}" STREQUAL "")
          install_symbol_file(${symbolFile} ${destination} COMPONENT ${INSTALL_CLR_COMPONENT})
        endif()
      elseif("${targetType}" STREQUAL "SHARED_LIBRARY")
        #imported shared lib - install the imported artifacts
        #imported static lib - nothing to install

        if ("${CMAKE_VERSION}" VERSION_LESS "3.21")
          install(PROGRAMS $<TARGET_PROPERTY:${targetName},IMPORTED_LOCATION> DESTINATION ${destination} COMPONENT ${INSTALL_CLR_COMPONENT})
        else()
          install(IMPORTED_RUNTIME_ARTIFACTS ${targetName} DESTINATION ${destination} COMPONENT ${INSTALL_CLR_COMPONENT})
        endif()
        if (NOT "${symbolFile}" STREQUAL "")
          install_symbol_file(${symbolFile} ${destination} COMPONENT ${INSTALL_CLR_COMPONENT})
        endif()
      endif()

      if(CLR_CMAKE_PGO_INSTRUMENT)
        if(WIN32)
          get_property(is_multi_config GLOBAL PROPERTY GENERATOR_IS_MULTI_CONFIG)
          if(is_multi_config)
              install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pgd DESTINATION ${destination}/PGD OPTIONAL COMPONENT ${INSTALL_CLR_COMPONENT})
          else()
              install(FILES ${CMAKE_CURRENT_BINARY_DIR}/${targetName}.pgd DESTINATION ${destination}/PGD OPTIONAL COMPONENT ${INSTALL_CLR_COMPONENT})
          endif()
        endif()
      endif()
    endforeach()
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
  # Disabling PAX hardening only makes sense in systems that use Elf image formats. Particularly, looking
  # for paxctl in macOS is problematic as it collides with popular software for that OS that performs completely
  # unrelated functionality. Only look for it when we'll generate Elf images.
  if (CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
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
  endif(CLR_CMAKE_HOST_LINUX OR CLR_CMAKE_HOST_FREEBSD OR CLR_CMAKE_HOST_NETBSD OR CLR_CMAKE_HOST_SUNOS)
endfunction()

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

function(link_natvis_sources_for_target targetName linkKind)
    if (NOT CLR_CMAKE_HOST_WIN32)
        return()
    endif()
    foreach(source ${ARGN})
        if (NOT IS_ABSOLUTE "${source}")
            convert_to_absolute_path(source ${source})
        endif()
        get_filename_component(extension "${source}" EXT)
        if ("${extension}" STREQUAL ".natvis")
            # Since natvis embedding is only supported on Windows
            # we can use target_link_options since our minimum version is high enough
            target_link_options(${targetName} "${linkKind}" "-NATVIS:${source}")
        endif()
    endforeach()
endfunction()

# Add sanitizer runtime support code to the target.
function(add_sanitizer_runtime_support targetName)
  # Add sanitizer support functions.
  if (CLR_CMAKE_ENABLE_ASAN)
    target_sources(${targetName} PRIVATE "$<$<STREQUAL:$<TARGET_PROPERTY:TYPE>,EXECUTABLE>:${CLR_SRC_NATIVE_DIR}/minipal/asansupport.cpp>")
  endif()
endfunction()

function(add_executable_clr targetName)
  if(NOT WIN32)
    add_executable(${ARGV} ${VERSION_FILE_PATH})
    disable_pax_mprotect(${ARGV})
  else()
    add_executable(${ARGV})
  endif(NOT WIN32)
  add_sanitizer_runtime_support(${targetName})
  if(NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS)
    strip_symbols(${ARGV0} symbolFile)
  endif()
endfunction()

function(add_library_clr targetName kind)
  if(NOT WIN32 AND "${kind}" STREQUAL "SHARED")
    add_library(${ARGV} ${VERSION_FILE_PATH})
  else()
    add_library(${ARGV})
  endif()
  if("${kind}" STREQUAL "SHARED" AND NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS)
    strip_symbols(${ARGV0} symbolFile)
  endif()
endfunction()

# Adhoc sign targetName with the entitlements in entitlementsFile.
function(adhoc_sign_with_entitlements targetName entitlementsFile)
    # Add a dependency from a source file for the target on the entitlements file to ensure that the target is rebuilt if only the entitlements file changes.
    get_target_property(sources ${targetName} SOURCES)
    list(GET sources 0 firstSource)
    set_source_files_properties(${firstSource} PROPERTIES OBJECT_DEPENDS ${entitlementsFile})

    add_custom_command(
        TARGET ${targetName}
        POST_BUILD
        COMMAND codesign -s - -f --entitlements ${entitlementsFile} $<TARGET_FILE:${targetName}>)
endfunction()
