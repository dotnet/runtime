project(metadata C)

#
# This library contains the icall tables if the runtime was configured with DISABLE_ICALL_TABLES
#
if(DISABLE_ICALL_TABLES)
add_library(mono-icall-table STATIC "icall-table.c")
target_link_libraries(mono-icall-table PRIVATE monoapi eglib_api)
target_include_directories(mono-icall-table PRIVATE ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_SOURCE_DIR}/../..
  ${PROJECT_SOURCE_DIR}/..)
install(TARGETS mono-icall-table LIBRARY)
else()
set(icall_table_sources "icall-table.c")
endif()

#
# This library contains runtime IL code generation
#
set(ilgen_base_sources
    method-builder-ilgen.c
    method-builder-ilgen.h
    method-builder-ilgen-internals.h
    marshal-lightweight.c
    marshal-lightweight.h
    marshal-shared.c
    marshal-shared.h
    sgen-mono-ilgen.c
    sgen-mono-ilgen.h)

set(ilgen_sources ${ilgen_base_sources})

set(metadata_win32_sources
    w32event-win32.c)

set(metadata_unix_sources
    w32event-unix.c)

if(HOST_WIN32)
set(metadata_platform_sources ${metadata_win32_sources})
else()
set(metadata_platform_sources ${metadata_unix_sources})
endif()

set(metadata_common_sources
    appdomain.c
    domain.c
    appdomain-icalls.h
    assembly.c
    assembly-internals.h
    bundled-resources.c
    bundled-resources-internals.h
    cil-coff.h
    class.c
    class-getters.h
    class-init.h
    class-init-internals.h
    class-init.c
    class-internals.h
    class-inlines.h
    class-private-definition.h
    class-accessors.c
    class-setup-vtable.c
    cominterop.c
    cominterop.h
    components.h
    components.c
    debug-helpers.c
    debug-mono-symfile.c
    debug-mono-ppdb.h
    debug-mono-ppdb.c
    domain-internals.h
    environment.c
    exception.c
    exception-internals.h
    gc-internals.h
    gc_wrapper.h
    icall.c
    icall-internals.h
    icall-def.h
    icall-table.h
    icall-eventpipe.c
    image.c
    image-internals.h
    webcil-loader.h
    webcil-loader.c
    jit-info.h
    jit-info.c
    loader.c
    lock-tracer.c
    lock-tracer.h
    marshal.c
    marshal.h
    marshal-internals.h
    mempool.c
    mempool.h
    mempool-internals.h
    metadata.c
    metadata-internals.h
    metadata-update.h
    metadata-update.c
    method-builder.h
    method-builder-internals.h
    method-builder.c
    mono-basic-block.c
    mono-basic-block.h
    mono-config.c
    mono-debug.c
    debug-internals.h
    mono-endian.c
    mono-endian.h
    mono-hash.c
    mono-hash.h
    mono-conc-hash.c
    mono-conc-hash.h
    weak-hash.c
    weak-hash.h
    mono-ptr-array.h
    monitor.h
    object.c
    object-internals.h
    opcodes.c
    property-bag.h
    property-bag.c
    profiler.c
    profiler-private.h
    runtime.c
    string-icalls.c
    string-icalls.h
    sysmath.c
    tabledefs.h
    threads.c
    threads-types.h
    verify.c
    wrapper-types.h
    dynamic-image-internals.h
    dynamic-stream.c
    dynamic-stream-internals.h
    reflection-cache.h
    custom-attrs-internals.h
    sre-internals.h
    reflection-internals.h
    object-offsets.h
    abi-details.h
    class-abi-details.h
    metadata-cross-helpers.c
    seq-points-data.h
    seq-points-data.c
    handle.c
    handle.h
    w32event.h
    w32handle.h
    w32handle.c
    reflection.c
    dynamic-image.c
    sre.c
    sre-encode.c
    custom-attrs.c
    callspec.h
    callspec.c
    assembly-load-context.c
    icall-eventpipe.c
    native-library.h
    native-library.c
    native-library-qcall.c
    loaded-images-internals.h
    loaded-images.c
    abi-details.h
    abi.c
    memory-manager.c
    unsafe-accessor.h
    unsafe-accessor.c
    icall-table.h
    ${icall_table_sources})

set(metadata_gc_dependent_sources
     gc-stats.c
     gc.c
     monitor.c)

if(MONO_GC STREQUAL "sgen")
    set(metadata_gc_sources
        sgen-bridge.c
        sgen-bridge-internals.h
        sgen-new-bridge.c
        sgen-tarjan-bridge.c
        sgen-toggleref.c
        sgen-toggleref.h
        sgen-stw.c
        sgen-mono.c
        sgen-mono.h
        sgen-client-mono.h)
    set(metadata_compile_definitions "HAVE_SGEN_GC")
elseif(MONO_GC STREQUAL "boehm")
    set(metadata_gc_sources
        boehm-gc.c)
    set(metadata_compile_definitions "HAVE_BOEHM_GC")
endif()

set(metadata_sources "${metadata_platform_sources};${metadata_common_sources};${metadata_gc_dependent_sources};${metadata_gc_sources};${ilgen_sources}")

if(HOST_WIN32 AND NOT DISABLE_SHARED_LIBS)
    add_library(metadata_objects_shared OBJECT ${metadata_sources})
    target_compile_definitions(metadata_objects_shared PRIVATE ${metadata_compile_definitions})
    target_link_libraries(metadata_objects_shared PRIVATE monoapi eglib_api utils_objects_shared netintrinsics)
    # note: metadata_objects is an object library, so this doesn't force linking with sgen,
    # it just adds the relevant include directories - which we need even with Boehm
    target_link_libraries(metadata_objects_shared PRIVATE sgen_objects_shared)
    target_compile_definitions(metadata_objects_shared PRIVATE -DMONO_DLL_EXPORT)
    target_include_directories(metadata_objects_shared PRIVATE ${PROJECT_BINARY_DIR}/../..
        ${PROJECT_SOURCE_DIR}/../..
        ${PROJECT_SOURCE_DIR}/..)
endif()

add_library(metadata_objects OBJECT ${metadata_sources})
target_compile_definitions(metadata_objects PRIVATE ${metadata_compile_definitions})
target_link_libraries(metadata_objects PRIVATE monoapi eglib_api utils_objects netintrinsics)
# note: metadata_objects is an object library, so this doesn't force linking with sgen,
# it just adds the relevant include directories - which we need even with Boehm
target_link_libraries(metadata_objects PRIVATE sgen_objects)
if(NOT HOST_WIN32)
    target_compile_definitions(metadata_objects PRIVATE -DMONO_DLL_EXPORT)
endif()
target_include_directories(metadata_objects PRIVATE ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_SOURCE_DIR}/../..
  ${PROJECT_SOURCE_DIR}/..)
