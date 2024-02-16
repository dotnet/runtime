
# Infrastructure to import a native aot compiled library into a native build

# If we're statically linking one or more NativeAOTed libraries, add a target that brings in the
# static library NativeAOT runtime components.  This function can be called more than once.  Each
# imported library should add its own dependency on the "nativeAotFramework" target
#
# This is only necessary if NativeAOT libraries are compiled to static native libraries (in which
# case they will share a single NativeAOT runtime). If all the NativeAOTed libraries are shared,
# they will each run in their own isolated NativeAOT runtime.
function(add_nativeAotFramework_targets_once)
  get_property(targets_added GLOBAL PROPERTY CLR_CMAKE_NATIVEAOTFRAMEWORK_TARGETS_ADDED)
  if (NOT "${targets_added}")
    add_library(nativeAotFramework INTERFACE)

    # depends on NATIVEAOT_FRAMEWORK_PATH and NATIVEAOT_SDK_PATH to be set
    # by the generated .cmake fragment during the managed build

    if ("${CLR_CMAKE_HOST_WIN32}")
      set(NAOT_SDK_BASE_BOOTSTRAP bootstrapperdll${CMAKE_C_OUTPUT_EXTENSION})
    else()
      set(NAOT_SDK_BASE_BOOTSTRAP libbootstrapperdll${CMAKE_C_OUTPUT_EXTENSION})
    endif()

    set(NAOT_SDK_BASE_LIBS
      Runtime.WorkstationGC
      eventpipe-disabled)

    if(NOT "${CLR_CMAKE_HOST_WIN32}")
      list(APPEND NAOT_SDK_BASE_LIBS stdc++compat)
    else()
      list(APPEND NAOT_SDK_BASE_LIBS
        Runtime.VxsortEnabled
        System.Globalization.Native.Aot
        System.IO.Compression.Native.Aot
      )
    endif()

    addprefix(NAOT_SDK_BOOTSTRAP "${NATIVEAOT_SDK_PATH}" "${NAOT_SDK_BASE_BOOTSTRAP}")
    addprefix(NAOT_SDK_LIBS "${NATIVEAOT_SDK_PATH}" "${NAOT_SDK_BASE_LIBS}")

    if("${CLR_CMAKE_HOST_WIN32}")
      set(NAOT_FRAMEWORK_BASE_LIBS)
    else()
      set(NAOT_FRAMEWORK_BASE_LIBS
        System.Native
        System.Globalization.Native
        System.IO.Compression.Native
        System.Net.Security.Native
        System.Security.Cryptography.Native.OpenSsl)
    endif()

    addprefix(NAOT_FRAMEWORK_LIBS "${NATIVEAOT_FRAMEWORK_PATH}" "${NAOT_FRAMEWORK_BASE_LIBS}")

    if("${CLR_CMAKE_HOST_WIN32}")
      list(TRANSFORM NAOT_FRAMEWORK_LIBS APPEND "${CMAKE_STATIC_LIBRARY_SUFFIX}")
      list(TRANSFORM NAOT_SDK_LIBS APPEND "${CMAKE_STATIC_LIBRARY_SUFFIX}")
    endif()

    if("${CLR_CMAKE_HOST_APPLE}")
      list(TRANSFORM NAOT_SDK_BASE_LIBS PREPEND "-Wl,-hidden-l" OUTPUT_VARIABLE NAOT_SDK_HIDDEN_LIBS)
      list(TRANSFORM NAOT_FRAMEWORK_BASE_LIBS PREPEND "-Wl,-hidden-l" OUTPUT_VARIABLE NAOT_FRAMEWORK_HIDDEN_LIBS)
      target_link_directories(nativeAotFramework INTERFACE "${NATIVEAOT_FRAMEWORK_PATH}" "${NATIVEAOT_SDK_PATH}")
      target_link_libraries(nativeAotFramework INTERFACE "${NAOT_SDK_BOOTSTRAP}" "${NAOT_SDK_HIDDEN_LIBS}" "${NAOT_FRAMEWORK_HIDDEN_LIBS}" -lm)
    elseif("${CLR_CMAKE_HOST_UNIX}")
      target_link_directories(nativeAotFramework INTERFACE "${NATIVEAOT_FRAMEWORK_PATH}" "${NATIVEAOT_SDK_PATH}")
      list(TRANSFORM NAOT_SDK_BASE_LIBS PREPEND "lib" OUTPUT_VARIABLE NAOT_SDK_HIDDEN_LIBS)
      list(TRANSFORM NAOT_FRAMEWORK_BASE_LIBS PREPEND "lib" OUTPUT_VARIABLE NAOT_FRAMEWORK_HIDDEN_LIBS)
      string(REPLACE ";" ":" NAOT_SDK_EXCLUDE_ARG "${NAOT_SDK_HIDDEN_LIBS}")
      string(REPLACE ";" ":" NAOT_FRAMEWORK_EXCLUDE_ARG "${NAOT_FRAMEWORK_HIDDEN_LIBS}")
      target_link_libraries(nativeAotFramework INTERFACE "${NAOT_SDK_BOOTSTRAP}" "${NAOT_SDK_BASE_LIBS}" "${NAOT_FRAMEWORK_BASE_LIBS}" -lm)
      target_link_options(nativeAotFramework INTERFACE "LINKER:--exclude-libs=${NAOT_SDK_EXCLUDE_ARG}:${NAOT_FRAMEWORK_EXCLUDE_ARG}")
      target_link_options(nativeAotFramework INTERFACE "LINKER:--discard-all")
      target_link_options(nativeAotFramework INTERFACE "LINKER:--gc-sections")
    elseif("${CLR_CMAKE_HOST_WIN32}")
      target_link_directories(nativeAotFramework INTERFACE "${NATIVEAOT_FRAMEWORK_PATH}" "${NATIVEAOT_SDK_PATH}")
      target_link_libraries(nativeAotFramework INTERFACE "${NAOT_SDK_BOOTSTRAP}" "${NAOT_SDK_LIBS}" "${NAOT_FRAMEWORK_LIBS}" BCrypt)
    endif()
    
    set_property(GLOBAL PROPERTY CLR_CMAKE_NATIVEAOTFRAMEWORK_TARGETS_ADDED 1)
  endif()
endfunction()

# see add_imported_nativeaot_library_clr, below
function(add_imported_nativeaot_library targetName symbolPrefix)
  message(STATUS "${symbolPrefix}_MODE is ${${symbolPrefix}_MODE}")
  if ("${${symbolPrefix}_MODE}" STREQUAL "SHARED")
    add_library(${targetName} INTERFACE)

    set(libName "${${symbolPrefix}_NAME}") # typically same as targetName
    set(libPath "${${symbolPrefix}_LIBPATH}") # typically /.../artifacts/bin/<libName>/<config>/<rid>/publish
    set(libFilename "${libName}${${symbolPrefix}_EXT}") # <libName>.dll, <libName>.so or <libName>.dylib
    set(libFullPath "${libPath}/${libFilename}")
    # windows import library
    set(libImpLibFullPath "${${symbolPrefix}_IMPLIBPATH}") # typically \...\artifacts\bin\<libName>\<config>\<rid>\native\<libName>.lib

    add_library(${targetName}-shared SHARED IMPORTED)
    set_property(TARGET ${targetName}-shared PROPERTY IMPORTED_LOCATION "${libFullPath}")

    if("${CLR_CMAKE_HOST_WIN32}")
      set_property(TARGET ${targetName}-shared PROPERTY IMPORTED_IMPLIB "${libImpLibFullPath}")
    endif()

    # TODO bake this into the cmake fragment?
    target_include_directories(${targetName}-shared INTERFACE "${CLR_SRC_NATIVE_DIR}/${libName}/inc")

    target_link_libraries(${targetName} INTERFACE ${targetName}-shared)

    # FIXME: this installs into artifacts/bin/coreclr/<triple>/lib/
    # we instead need to add a IMPORTED_RUNTIME_ARTIFACTS mode for install_clr() so that it installs
    #  with the component
    if("${CLR_CMAKE_HOST_WIN32}")
      install(IMPORTED_RUNTIME_ARTIFACTS ${targetName}-shared RUNTIME) # dlls are "runtime" on Windows
    else()
      install(IMPORTED_RUNTIME_ARTIFACTS ${targetName}-shared LIBRARY) # .so/.dylib are "library"
    endif()

  elseif ("${${symbolPrefix}_MODE}" STREQUAL "STATIC")
    add_nativeAotFramework_targets_once()

    set(libName "${${symbolPrefix}_NAME}") # typically same as targetName
    set(libPath "${${symbolPrefix}_LIBPATH}") # typically /.../artifacts/bin/<libName>/<config>/<rid>/publish
    set(libFilename "${libName}${${symbolPrefix}_EXT}") # targetName.a or targetName.lib

    # FIXME: annoyingly cmake doesn't treat changes at IMPORTED_LOCATION or target_link_libraries as
    # a trigger to rebuild the downstream targets.  we might need to use an add_custom_command that
    # pretends the library is a BYPRODUCTS of the custom command.  although that will cause the
    # library to be deleted if we ever run "make clean".  Maybe that's what we want.

    # what we're trying to achieve is that all the symbols from the static library are hidden in the
    # final shared library or executable target.  If we had object files, the normal GCC/Clang
    # "-fvisibility=hidden" mechanism would hide all the non-exported symbols.  Or if we had an
    # exported symbols list we could use an LD version script or the apple -exported_symbols_list
    # option.  But we dont' have that, so instead we use the GNU LD `--exclude-libs=libtargetName.a` option, or the  apple `-hidden-ltargetName` option

    if("${CLR_CMAKE_HOST_APPLE}")
      message(STATUS creeating ${targetName}-static for apple)
      # hack: -hidden-l wants the library name without a "lib" prefix
      STRING(REGEX REPLACE "^lib" "" libBaseName ${libName})
      add_library(${targetName}-static INTERFACE IMPORTED)
      target_link_directories(${targetName}-static INTERFACE "${libPath}")
      target_link_libraries(${targetName}-static INTERFACE "-Wl,-hidden-l${libBaseName}")
    elseif("${CLR_CMAKE_HOST_UNIX}")
      add_library(${targetName}-static STATIC IMPORTED)
      set_property(TARGET ${targetName}-static PROPERTY IMPORTED_LOCATION "${libPath}/${libFilename}")
      target_link_options(${targetName}-static INTERFACE "LINKER:--exclude-libs=${libFilename}")
    elseif("${CLR_CMAKE_HOST_WIN32}")
      add_library(${targetName}-static STATIC IMPORTED)
      set_property(TARGET ${targetName}-static PROPERTY IMPORTED_LOCATION "${libPath}\\${libFilename}")
    endif()

    # TODO bake this into the cmake fragment?
    target_include_directories(${targetName}-static INTERFACE "${CLR_SRC_NATIVE_DIR}/${libName}/inc")
    target_link_libraries(${targetName}-static INTERFACE nativeAotFramework)

    add_library(${targetName} INTERFACE)
    target_link_libraries(${targetName} INTERFACE ${targetName}-static)
  else()
    message(FATAL_ERROR "${symbolPrefix}_MODE must be one of SHARED or STATIC")
  endif()
endfunction()

# adds a target targetName that can be used to reference a NativeAOTed library.  symbolPrefix should
# be the ALLCAPS prefix of the variables that define the mode and paths for the library.  They are
# typically set in the artifacts/obj/targetName/targetName.cmake fragment which should be included
# before calling this function.
function(add_imported_nativeaot_library_clr targetName symbolPrefix)
  add_imported_nativeaot_library(${ARGV})
  # FIXME: target xyz-shared is imported and does not build here
  #if ("${${symbolPrefix}_MODE}" STREQUAL "SHARED" AND NOT CLR_CMAKE_KEEP_NATIVE_SYMBOLS)
  #  strip_symbols("${ARGV0}-shared" symbolFile)
  #endif()
endfunction()

# TODO: copy the shared lib to the correct output folder
#if("${LIBNAOTHELLO_MODE}" STREQUAL "shared")
#  add_custom_command(TARGET helloLib POST_BUILD
#    COMMAND "${CMAKE_COMMAND}" -E copy_if_different "$<TARGET_FILE:naothello-shared>" "$<TARGET_FILE_DIR:helloLib>")
#endif()
