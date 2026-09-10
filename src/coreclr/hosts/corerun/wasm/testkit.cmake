# Licensed to the .NET Foundation under one or more agreements.
# The .NET Foundation licenses this file to you under the MIT license.

# Corerun test link kit (browser-wasm only).
#
# WASM has no dynamic library loading, so a runtime test that P/Invokes into its
# own native library cannot run against the prebuilt corerun.js -- the test's
# native code, and the call tables generated for it, have to be linked into the
# host itself. The runtime test build therefore re-links a test-specific corerun
# outside of CMake (see src/tests/Common/CLRTest.WasmCorerun.targets).
#
# This module exports everything that re-link needs:
#   * libcorerun_static.a -- corerun's own objects, so the host does not have to
#     be recompiled per test.
#   * every static archive corerun links against, except coreclr_gen_static
#     (the checked-in framework call tables), which the per-test build replaces
#     with tables generated for that test's assemblies.
#   * the JavaScript libraries corerun links in.
#   * the headers the generated call tables include.
#   * response files carrying the exact compile and link flags CMake would use.
#
# Paths inside the response files are written relative to the kit directory
# rather than as absolute paths: coreclr and the runtime tests are built in
# separate CI jobs, so the kit is consumed from wherever the build artifact was
# unpacked. Its consumer runs em++ with the kit directory as the working
# directory and can therefore hand the response files straight to the compiler.
#
# The kit is verified by reproducing corerun itself from it: compiling the
# checked-in call tables in src/coreclr/vm/wasm/browser with corerun-compile.rsp
# and linking them with corerun-link.rsp yields a working corerun.

# Collect every link input reachable from ${target}'s link graph, ordered so that
# a dependent always precedes its dependencies (the order wasm-ld needs for
# single-pass archive resolution). Implemented as a depth-first post-order walk
# that is reversed at the end; global properties carry state across recursion.
#
# The result is a single ordered list whose entries are tagged, because all three
# kinds share one ordering:
#   TARGET:<name>  a static library built in this tree
#   FILE:<path>    a prebuilt archive linked by absolute path
#   FLAG:<text>    anything the linker takes verbatim, such as -lstdc++
function(_corerun_kit_visit target)
    get_property(visited GLOBAL PROPERTY _corerun_kit_visited)
    if(target IN_LIST visited)
        return()
    endif()
    set_property(GLOBAL APPEND PROPERTY _corerun_kit_visited "${target}")

    if(NOT TARGET ${target})
        return()
    endif()

    # Object and interface libraries carry dependencies of their own that end up
    # on the link line, so they have to be walked through even though they do not
    # contribute an archive themselves. Shared and module libraries are dead ends:
    # nothing behind them is statically linked.
    get_target_property(targetType ${target} TYPE)
    if(targetType STREQUAL "SHARED_LIBRARY" OR targetType STREQUAL "MODULE_LIBRARY")
        return()
    endif()

    set(dependencies "")
    foreach(property IN ITEMS LINK_LIBRARIES INTERFACE_LINK_LIBRARIES)
        get_target_property(value ${target} ${property})
        if(value)
            list(APPEND dependencies ${value})
        endif()
    endforeach()

    foreach(dependency IN LISTS dependencies)
        # PRIVATE links on a static library surface as $<LINK_ONLY:dep>.
        if(dependency MATCHES "^\\$<LINK_ONLY:(.+)>$")
            set(dependency "${CMAKE_MATCH_1}")
        endif()
        # coreclr_static references the server GC as $<TARGET_NAME_IF_EXISTS:vm_gc_svr>
        # because that target does not exist on every platform, wasm included.
        if(dependency MATCHES "^\\$<TARGET_NAME_IF_EXISTS:(.+)>$")
            set(dependency "${CMAKE_MATCH_1}")
            if(NOT TARGET ${dependency})
                continue()
            endif()
        endif()
        if(TARGET ${dependency})
            _corerun_kit_visit(${dependency})
        elseif(dependency MATCHES "^\\$<")
            # Generator expressions cannot be resolved at configure time. None are
            # expected on the browser link line, so fail loudly rather than
            # silently shipping a kit that is missing a link input.
            message(FATAL_ERROR
                "corerun test kit: cannot resolve generator expression '${dependency}' "
                "reached from target '${target}'.")
        elseif(dependency MATCHES "^-")
            set_property(GLOBAL APPEND PROPERTY _corerun_kit_order "FLAG:${dependency}")
        elseif(IS_ABSOLUTE "${dependency}")
            # A prebuilt archive linked by path rather than by target, such as the
            # ICU libraries pulled in from a NuGet package by link_libraries().
            # Those paths come from MSBuild, so on a Windows host they arrive with
            # backslashes. install() writes them into cmake_install.cmake as quoted
            # strings, where a backslash is an escape, so normalize them here.
            # file(TO_CMAKE_PATH) is not usable: off Windows it also splits on ':'
            # and would turn a drive letter into a list separator.
            string(REPLACE "\\" "/" dependency "${dependency}")
            set_property(GLOBAL APPEND PROPERTY _corerun_kit_order "FILE:${dependency}")
        else()
            # A bare library name. CMake renders these as -l<name>; stdc++ arrives
            # this way from System.Globalization.Native's link_libraries().
            set_property(GLOBAL APPEND PROPERTY _corerun_kit_order "FLAG:-l${dependency}")
        endif()
    endforeach()

    if(targetType STREQUAL "STATIC_LIBRARY")
        set_property(GLOBAL APPEND PROPERTY _corerun_kit_order "TARGET:${target}")
    endif()
endfunction()

# Returns the tagged, ordered link input list described above.
function(corerun_kit_collect_link_inputs target outVar)
    set_property(GLOBAL PROPERTY _corerun_kit_visited "")
    set_property(GLOBAL PROPERTY _corerun_kit_order "")
    _corerun_kit_visit(${target})
    get_property(order GLOBAL PROPERTY _corerun_kit_order)
    # Deduplicate before reversing. REMOVE_DUPLICATES keeps the first occurrence,
    # and in a post-order walk that is the deepest one -- it precedes every target
    # that depends on it, so reversing moves it after all of them, which is where
    # a single-pass linker needs it. Deduplicating after the reverse would instead
    # keep the shallowest position and hoist the input ahead of its dependents.
    # Only FILE: and FLAG: entries can repeat; targets are already unique through
    # _corerun_kit_visited.
    list(REMOVE_DUPLICATES order)
    list(REVERSE order)
    set(${outVar} "${order}" PARENT_SCOPE)
endfunction()

# Export the kit: corerun's objects, its static archives, its JavaScript
# libraries, the headers the generated call tables include, and the response
# files describing how to compile and link them.
#
#   COMPONENT       -- the install component the kit belongs to
#   SOURCES         -- corerun's own sources (compiled into libcorerun_static.a)
#   LINK_OPTIONS    -- the emcc link options this directory adds to corerun; the
#                      ones it inherits are picked up from the directory property
#   JS_LIBRARIES    -- --js-library inputs
#   EXTERN_POST_JS  -- --extern-post-js input
function(corerun_kit_export)
    cmake_parse_arguments(KIT "" "COMPONENT;EXTERN_POST_JS" "SOURCES;LINK_OPTIONS;JS_LIBRARIES" ${ARGN})

    set(kitDestination "corerun-link")

    # corerun's objects, so a per-test relink does not have to recompile the host.
    # Mirroring corerun's link libraries is what makes the archive interchangeable
    # with corerun's own objects: the usage requirements of those libraries (their
    # interface compile definitions and include directories) have to be in scope
    # when these sources are compiled, exactly as they are for corerun.
    add_library_clr(corerun_static STATIC ${KIT_SOURCES})
    target_include_directories(corerun_static PRIVATE ${CMAKE_CURRENT_SOURCE_DIR}/wasm/)
    get_target_property(corerunLinkLibraries corerun LINK_LIBRARIES)
    target_link_libraries(corerun_static PRIVATE ${corerunLinkLibraries})

    corerun_kit_collect_link_inputs(corerun kitInputs)

    # Split the tagged list back out, dropping coreclr_gen_static: it holds the
    # checked-in framework call tables, and the per-test link replaces it with
    # tables generated for that test.
    set(kitLibraries "")
    set(kitFiles "")
    set(kitLinkInputs "")
    foreach(input IN LISTS kitInputs)
        if(input MATCHES "^TARGET:(.+)$")
            if(CMAKE_MATCH_1 STREQUAL "coreclr_gen_static")
                continue()
            endif()
            list(APPEND kitLibraries "${CMAKE_MATCH_1}")
            list(APPEND kitLinkInputs "\"$<TARGET_FILE_NAME:${CMAKE_MATCH_1}>\"")
        elseif(input MATCHES "^FILE:(.+)$")
            list(APPEND kitFiles "${CMAKE_MATCH_1}")
            get_filename_component(fileName "${CMAKE_MATCH_1}" NAME)
            list(APPEND kitLinkInputs "\"${fileName}\"")
        elseif(input MATCHES "^FLAG:(.+)$")
            list(APPEND kitLinkInputs "${CMAKE_MATCH_1}")
        endif()
    endforeach()

    list(PREPEND kitLibraries corerun_static)
    list(PREPEND kitLinkInputs "\"$<TARGET_FILE_NAME:corerun_static>\"")

    # Nothing links corerun_static, so without this the component target would
    # try to install an archive that was never built.
    add_dependencies(${KIT_COMPONENT} ${kitLibraries})

    # corerun is linked with more than the options this directory appends to it:
    # the wasm exception model and the emscripten sysroot libraries come from
    # add_link_options() higher up (eng/native/configureplatform.cmake), and
    # --gc-sections from the per-configuration linker flags. Leaving any of them
    # out produces a corerun that either fails to link or behaves differently from
    # the one CMake builds, so take all three sources rather than just this
    # directory's list.
    #
    # Everything collected here has to be relocatable -- no absolute path may
    # reach the response file, because the kit is consumed from a different
    # machine than the one that built it. Paths into the kit are therefore
    # written relative to it, which is the working directory the consumer runs
    # em++ from.
    get_directory_property(inheritedLinkOptions LINK_OPTIONS)
    string(TOUPPER "${CMAKE_BUILD_TYPE}" configurationName)
    separate_arguments(configuredLinkerFlags NATIVE_COMMAND
        "${CMAKE_EXE_LINKER_FLAGS} ${CMAKE_EXE_LINKER_FLAGS_${configurationName}}")

    set(linkResponse "")
    foreach(option IN LISTS configuredLinkerFlags inheritedLinkOptions KIT_LINK_OPTIONS)
        string(APPEND linkResponse "${option}\n")
    endforeach()
    foreach(jsLibrary IN LISTS KIT_JS_LIBRARIES)
        get_filename_component(jsName "${jsLibrary}" NAME)
        string(APPEND linkResponse "--js-library\n\"${jsName}\"\n")
    endforeach()
    get_filename_component(externPostName "${KIT_EXTERN_POST_JS}" NAME)
    string(APPEND linkResponse "--extern-post-js\n\"${externPostName}\"\n")
    foreach(input IN LISTS kitLinkInputs)
        string(APPEND linkResponse "${input}\n")
    endforeach()

    # The generated call tables have to be compiled compatibly with corerun's own
    # objects -- the exception model in particular has to agree or wasm-ld
    # rejects the mix. These are the same flags the app-side relink uses for
    # these files (see BrowserWasmApp.CoreCLR.targets), and they match how the
    # checked-in framework call tables are compiled in tree. The repo's own
    # CMAKE_CXX_FLAGS are deliberately not forwarded: this build supplies its
    # compile flags through add_compile_options(), so those variables are empty
    # or, worse, hold per-configuration defaults the repo never actually uses.
    # corerun's include directories are likewise not exported: the generated
    # sources only need the headers shipped in the kit.
    set(compileResponse "")
    string(APPEND compileResponse "-O2\n")
    string(APPEND compileResponse "-fwasm-exceptions\n")
    string(APPEND compileResponse "-sWASM_LEGACY_EXCEPTIONS=0\n")
    string(APPEND compileResponse "-msimd128\n")
    string(APPEND compileResponse "-DGEN_PINVOKE=1\n")
    string(APPEND compileResponse "-I\"include\"\n")
    string(APPEND compileResponse "-include\n\"include/coreclr_compat.h\"\n")

    set(generatedDirectory "${CMAKE_CURRENT_BINARY_DIR}/corerun-link")
    file(GENERATE OUTPUT "${generatedDirectory}/corerun-link.rsp" CONTENT "${linkResponse}"
         TARGET corerun_static)
    file(GENERATE OUTPUT "${generatedDirectory}/corerun-compile.rsp" CONTENT "${compileResponse}"
         TARGET corerun_static)

    foreach(library IN LISTS kitLibraries)
        install(FILES "$<TARGET_FILE:${library}>" DESTINATION ${kitDestination} COMPONENT ${KIT_COMPONENT})
    endforeach()
    install(FILES ${kitFiles} DESTINATION ${kitDestination} COMPONENT ${KIT_COMPONENT})
    install(FILES ${KIT_JS_LIBRARIES} "${KIT_EXTERN_POST_JS}" DESTINATION ${kitDestination} COMPONENT ${KIT_COMPONENT})
    install(FILES "${generatedDirectory}/corerun-link.rsp" "${generatedDirectory}/corerun-compile.rsp"
            DESTINATION ${kitDestination} COMPONENT ${KIT_COMPONENT})

    # Headers the generated call tables include, plus the compatibility header
    # that supplies the CoreCLR prerequisites they expect to be in scope. That
    # header is the one the app-side relink force-includes, shared rather than
    # copied -- WasiApp.CoreCLR.targets reaches for it the same way.
    install(FILES
                "${CLR_DIR}/vm/wasm/callhelpers.hpp"
                "${CLR_REPO_ROOT_DIR}/src/mono/browser/build/coreclr_compat.h"
            DESTINATION ${kitDestination}/include COMPONENT ${KIT_COMPONENT})
    install(FILES
                "${CLR_SRC_NATIVE_DIR}/minipal/entrypoints.h"
                "${CLR_SRC_NATIVE_DIR}/minipal/utils.h"
            DESTINATION ${kitDestination}/include/minipal COMPONENT ${KIT_COMPONENT})
endfunction()
