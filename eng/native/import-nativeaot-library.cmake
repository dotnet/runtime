# Infrastructure to import a native aot compiled library into a native build

# ## Implementation details used by libraryName-config.cmake find_package configuration
# ## files. Consumers don't need to do this directly.

# add_imported_nativeaot_library(targetName symbolPrefix [NAMESPACE namespace])
#
# adds a target targetName that can be used to reference a NativeAOTed library.  symbolPrefix should
# be the ALLCAPS prefix of the variables that define the mode and paths for the library.  They are
# typically set in the artifacts/obj/targetName/targetName.cmake fragment which should be included
# before calling this function.
function(add_imported_nativeaot_library targetNameIn symbolPrefix)
  cmake_parse_arguments(PARSE_ARGV 2 "add_imported_opt" "" "NAMESPACE" "")
  message(TRACE "${symbolPrefix}_MODE is ${${symbolPrefix}_MODE}")
  set(targetName "${add_imported_opt_NAMESPACE}${targetNameIn}")
  message(TRACE "Adding target ${targetName}")

  if("${${symbolPrefix}_MODE}" STREQUAL "SHARED")
    set(libName "${${symbolPrefix}_NAME}") # typically same as targetName
    set(libPath "${${symbolPrefix}_LIBPATH}") # typically /.../artifacts/bin/<libName>/<config>/<rid>/publish
    set(libFilename "${libName}${${symbolPrefix}_EXT}") # <libName>.dll, <libName>.so or <libName>.dylib
    set(libFullPath "${libPath}/${libFilename}")

    if(CLR_CMAKE_HOST_WIN32)
      set(libPdbFullPath "${libPath}/${libName}.pdb")
    endif()

    # windows import library
    set(libImpLibFullPath "${${symbolPrefix}_IMPLIBPATH}") # typically /.../artifacts/bin/<libName>/<config>/<rid>/native/<libName>.lib

    # Copy the imported library into our binary dir.
    # We do this so that we may strip it and also so that cmake dependency tracking will be aware if the file changes
    set(importedFullPath "${CMAKE_CURRENT_BINARY_DIR}/imported_nativeaot_library/${targetNameIn}/${libFilename}")

    add_custom_command(OUTPUT "${importedFullPath}"
      DEPENDS "${libFullPath}"
      COMMAND ${CMAKE_COMMAND} -E copy_if_different "${libFullPath}" "${importedFullPath}"
    )

    if(CLR_CMAKE_HOST_WIN32)
      set(importedPdbFullPath "${CMAKE_CURRENT_BINARY_DIR}/imported_nativeaot_library/${targetNameIn}/${libName}.pdb")
      message(STATUS "Copying ${libPdbFullPath} to ${importedPdbFullPath} for ${targetName}")
      add_custom_command(OUTPUT "${importedPdbFullPath}"
        DEPENDS "${libPdbFullPath}"
        COMMAND ${CMAKE_COMMAND} -E copy_if_different "${libPdbFullPath}" "${importedPdbFullPath}")
    endif()

    # now make a custom target that other targets can depend on
    set(copy_target_name "${targetNameIn}_copy_imported_library")

    if(CLR_CMAKE_HOST_WIN32)
      add_custom_target("${copy_target_name}" DEPENDS "${importedFullPath}" "${importedPdbFullPath}")
    else()
      add_custom_target("${copy_target_name}" DEPENDS "${importedFullPath}")
    endif()

    add_library(${targetName} SHARED IMPORTED GLOBAL)
    add_dependencies(${targetName} "${copy_target_name}")
    set_property(TARGET ${targetName} PROPERTY IMPORTED_LOCATION "${importedFullPath}")
    set_property(TARGET ${targetName} PROPERTY CLR_IMPORTED_NATIVEAOT_LIBRARY 1)
    set_property(TARGET ${targetName} PROPERTY CLR_IMPORTED_COPY_TARGET "${copy_target_name}")

    if("${CLR_CMAKE_HOST_WIN32}")
      set_property(TARGET ${targetName} PROPERTY IMPORTED_IMPLIB "${libImpLibFullPath}")
    endif()

    set(libIncludePath "${${symbolPrefix}_INCLUDE}")
    target_include_directories(${targetName} INTERFACE "${libIncludePath}")

    strip_symbols(${targetName} symbolFile)

  else()
    message(FATAL_ERROR "${symbolPrefix}_MODE must be SHARED")
  endif()
endfunction()
