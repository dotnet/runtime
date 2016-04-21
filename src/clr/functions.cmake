function(clr_unknown_arch)
    if (WIN32)
        message(FATAL_ERROR "Only AMD64, ARM64 and I386 are supported")
    else()
        message(FATAL_ERROR "Only AMD64, ARM64 and ARM are supported")
    endif()
endfunction()

# Build a list of compiler definitions by putting -D in front of each define.
function(get_compile_definitions DefinitionName)
    # Get the current list of definitions
    get_directory_property(COMPILE_DEFINITIONS_LIST COMPILE_DEFINITIONS)

    foreach(DEFINITION IN LISTS COMPILE_DEFINITIONS_LIST)
        if (${DEFINITION} MATCHES "^\\$<\\$<CONFIG:([^>]+)>:([^>]+)>$")
            # The entries that contain generator expressions must have the -D inside of the
            # expression. So we transform e.g. $<$<CONFIG:Debug>:_DEBUG> to $<$<CONFIG:Debug>:-D_DEBUG>
            set(DEFINITION "$<$<CONFIG:${CMAKE_MATCH_1}>:-D${CMAKE_MATCH_2}>")
        else()
            set(DEFINITION -D${DEFINITION})
        endif()
        list(APPEND DEFINITIONS ${DEFINITION})
    endforeach()
    set(${DefinitionName} ${DEFINITIONS} PARENT_SCOPE)
endfunction(get_compile_definitions)

# Build a list of include directories by putting -I in front of each include dir.
function(get_include_directories IncludeDirectories)
    get_directory_property(dirs INCLUDE_DIRECTORIES)
    foreach(dir IN LISTS dirs)
        list(APPEND INC_DIRECTORIES -I${dir})
    endforeach()
    set(${IncludeDirectories} ${INC_DIRECTORIES} PARENT_SCOPE)
endfunction(get_include_directories)

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

#Preprocess exports definition file
function(preprocess_def_file inputFilename outputFilename)
  get_compile_definitions(PREPROCESS_DEFINITIONS)

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${CMAKE_CXX_COMPILER} /P /EP /TC ${PREPROCESS_DEFINITIONS}  /Fi${outputFilename}  ${inputFilename}
    DEPENDS ${inputFilename}
    COMMENT "Preprocessing ${inputFilename}"
  )

  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

function(generate_exports_file inputFilename outputFilename)

  if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
    set(AWK_SCRIPT generateexportedsymbols.awk)
  else()
    set(AWK_SCRIPT generateversionscript.awk)
  endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

  add_custom_command(
    OUTPUT ${outputFilename}
    COMMAND ${AWK} -f ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT} ${inputFilename} >${outputFilename}
    DEPENDS ${inputFilename} ${CMAKE_SOURCE_DIR}/${AWK_SCRIPT}
    COMMENT "Generating exports file ${outputFilename}"
  )
  set_source_files_properties(${outputFilename}
                              PROPERTIES GENERATED TRUE)
endfunction()

function(add_precompiled_header header cppFile targetSources)
  if(MSVC)
    set(precompiledBinary "${CMAKE_CURRENT_BINARY_DIR}/${CMAKE_CFG_INTDIR}/stdafx.pch")

    set_source_files_properties(${cppFile}
                                PROPERTIES COMPILE_FLAGS "/Yc\"${header}\" /Fp\"${precompiledBinary}\""
                                           OBJECT_OUTPUTS "${precompiledBinary}")
    set_source_files_properties(${${targetSources}}
                                PROPERTIES COMPILE_FLAGS "/Yu\"${header}\" /Fp\"${precompiledBinary}\""
                                           OBJECT_DEPENDS "${precompiledBinary}")
    # Add cppFile to SourcesVar
    set(${targetSources} ${${targetSources}} ${cppFile} PARENT_SCOPE)
  endif(MSVC)
endfunction()

function(strip_symbols targetName outputFilename)
  if(CLR_CMAKE_PLATFORM_UNIX)
    if(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)

      # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE
      # generator expression doesn't work correctly returning the wrong path and on
      # the newer cmake versions the LOCATION property isn't supported anymore.
      if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)
          set(strip_source_file $<TARGET_FILE:${targetName}>)
      else()
          get_property(strip_source_file TARGET ${targetName} PROPERTY LOCATION)
      endif()

      if(CMAKE_SYSTEM_NAME STREQUAL Darwin)
        set(strip_destination_file ${strip_source_file}.dwarf)

        add_custom_command(
          TARGET ${targetName}
          POST_BUILD
          VERBATIM 
          COMMAND ${DSYMUTIL} --flat --minimize ${strip_source_file}
          COMMAND ${STRIP} -u -r ${strip_source_file}
          COMMENT Stripping symbols from ${strip_source_file} into file ${strip_destination_file}
        )
      elseif(CMAKE_SYSTEM_NAME STREQUAL Linux)
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
      endif(CMAKE_SYSTEM_NAME STREQUAL Darwin)

      set(${outputFilename} ${strip_destination_file} PARENT_SCOPE)
    endif(UPPERCASE_CMAKE_BUILD_TYPE STREQUAL RELEASE)
  endif(CLR_CMAKE_PLATFORM_UNIX)
endfunction()

function(install_clr targetName)
  list(FIND CLR_CROSS_COMPONENTS_LIST ${targetName} INDEX)  
  if (NOT DEFINED CLR_CROSS_COMPONENTS_LIST OR NOT ${INDEX} EQUAL -1)  
    strip_symbols(${targetName} strip_destination_file) 
    # On the older version of cmake (2.8.12) used on Ubuntu 14.04 the TARGET_FILE  
    # generator expression doesn't work correctly returning the wrong path and on  
    # the newer cmake versions the LOCATION property isn't supported anymore.  
    if(CMAKE_VERSION VERSION_EQUAL 3.0 OR CMAKE_VERSION VERSION_GREATER 3.0)  
       set(install_source_file $<TARGET_FILE:${targetName}>)  
    else()  
        get_property(install_source_file TARGET ${targetName} PROPERTY LOCATION)  
    endif()  
  
    install(PROGRAMS ${install_source_file} DESTINATION .)  
    if(WIN32)  
        install(FILES ${CMAKE_CURRENT_BINARY_DIR}/$<CONFIG>/${targetName}.pdb DESTINATION PDB)  
    else()  
        install(FILES ${strip_destination_file} DESTINATION .)  
    endif()  
  endif()  
endfunction()

function(_add_executable)
    if(NOT WIN32)
      add_executable(${ARGV} ${VERSION_FILE_PATH})
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