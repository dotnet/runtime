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

    set(copy_target_name "${targetNameIn}_copy_imported_library")

    add_imported_library_clr(${targetName} COPY_TARGET ${copy_target_name} IMPORTED_LOCATION "${libFullPath}")
    set_property(TARGET ${targetName} PROPERTY CLR_IMPORTED_NATIVEAOT_LIBRARY 1)

    if("${CLR_CMAKE_HOST_WIN32}")
      set_property(TARGET ${targetName} PROPERTY IMPORTED_IMPLIB "${libImpLibFullPath}")
    endif()

    set(libIncludePath "${${symbolPrefix}_INCLUDE}")
    target_include_directories(${targetName} INTERFACE "${libIncludePath}")
  else()
    message(FATAL_ERROR "${symbolPrefix}_MODE must be SHARED")
  endif()
endfunction()
