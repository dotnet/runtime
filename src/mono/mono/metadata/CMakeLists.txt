#
# This library contains the icall tables if the runtime was configured with DISABLE_ICALL_TABLES
#
if(DISABLE_ICALL_TABLES)
add_library(mono-icall-table STATIC "../metadata/icall-table.c")
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
    marshal-ilgen.c
    marshal-ilgen.h
    sgen-mono-ilgen.c
    sgen-mono-ilgen.h)

if(NOT ENABLE_ILGEN)
addprefix(mono_ilgen_sources ../metadata "${ilgen_base_sources}")
set_source_files_properties(${mono_ilgen_sources} PROPERTIES COMPILE_DEFINITIONS "HAVE_SGEN_GC")
add_library(mono-ilgen STATIC "${mono_ilgen_sources}")
install(TARGETS mono-ilgen LIBRARY)
else()
set(ilgen_sources ${ilgen_base_sources})
endif()

set(metadata_win32_sources
    w32file-win32.c
    w32event-win32.c
    w32error-win32.c)

set(metadata_unix_sources
    w32event-unix.c
    w32process-unix-osx.c
    w32process-unix-bsd.c
    w32process-unix-haiku.c
    w32process-unix-default.c
    w32file-unix.c
    w32error-unix.c)

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
    coree.c
    coree.h
    coree-internals.h
    debug-helpers.c
    debug-mono-symfile.h
    debug-mono-symfile.c
    debug-mono-ppdb.h
    debug-mono-ppdb.c
    domain-internals.h
    environment.c
    environment.h
    exception.c
    exception.h
    exception-internals.h
    w32file.h
    gc-internals.h
    icall.c
    icall-internals.h
    icall-def.h
    icall-table.h
    icall-eventpipe.c
    image.c
    image-internals.h
    jit-info.h
    jit-info.c
    loader.c
    lock-tracer.c
    lock-tracer.h
    marshal.c
    marshal.h
    marshal-internals.h
    marshal-noilgen.c
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
    mono-debug.h
    mono-debug.c
    debug-internals.h
    mono-endian.c
    mono-endian.h
    mono-hash.c
    mono-hash.h
    mono-conc-hash.c
    mono-conc-hash.h
    mono-perfcounters.c
    mono-perfcounters.h
    mono-perfcounters-def.h
    mono-ptr-array.h
    monitor.h
    object.c
    object-forward.h
    object-internals.h
    opcodes.c
    property-bag.h
    property-bag.c
    w32process.h
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
    w32error.h
    reflection.c
    dynamic-image.c
    sre.c
    sre-encode.c
    custom-attrs.c
    fdhandle.h
    fdhandle.c
    callspec.h
    callspec.c
    assembly-load-context.c
    icall-eventpipe.c
    native-library.h
    native-library.c
    native-library-qcall.c
    qcall-def.h
    loaded-images-internals.h
    loaded-images.c
    abi-details.h
    abi.c
    memory-manager.c
    mono-config-dirs.h
    mono-config-dirs.c
    icall-table.h
    ${icall_table_sources})

set(metadata_gc_dependent_sources
     gc-stats.c
     gc.c
     monitor.c)

set(metadata_sgen_sources
    sgen-bridge.c
    sgen-bridge.h
    sgen-bridge-internals.h
    sgen-old-bridge.c
    sgen-new-bridge.c
    sgen-tarjan-bridge.c
    sgen-toggleref.c
    sgen-toggleref.h
    sgen-stw.c
    sgen-mono.c
    sgen-mono.h
    sgen-client-mono.h)

addprefix(metadata_gc_dependent_real_sources ../metadata "${metadata_gc_dependent_sources}")
addprefix(metadata_sgen_real_sources ../metadata "${metadata_sgen_sources}")
addprefix(ilgen_real_sources ../metadata "${ilgen_sources}")
set_source_files_properties(${metadata_sgen_real_sources};${metadata_gc_dependent_real_sources};${ilgen_real_sources} PROPERTIES COMPILE_DEFINITIONS "HAVE_SGEN_GC")

#
# We localize the usage of MONO_BINDIR etc. to just one source file, thus enabling
# ccache to work even if the value of these defines change.
#
set(bindir "${CMAKE_INSTALL_FULL_BINDIR}")
set(assembliesdir "${CMAKE_INSTALL_FULL_LIBDIR}")
set(confdir "${CMAKE_INSTALL_FULL_SYSCONFDIR}")
set(reloc_libdir "${CMAKE_INSTALL_LIBDIR}")

set_source_files_properties(../metadata/mono-config-dirs.c PROPERTIES COMPILE_DEFINITIONS "MONO_BINDIR=\"${bindir}\";MONO_ASSEMBLIES=\"${assembliesdir}\";MONO_CFG_DIR=\"${confdir}\";MONO_RELOC_LIBDIR=\"../${reloc_libdir}\"")

addprefix(metadata_sources ../metadata/ "${metadata_platform_sources};${metadata_common_sources};${metadata_gc_dependent_sources};${metadata_sgen_sources};${ilgen_sources}")

set(metadata_public_headers_base
	appdomain.h
	assembly.h
	attrdefs.h
	blob.h
	class.h
	debug-helpers.h
	debug-mono-symfile.h
	environment.h
	exception.h
	image.h
	loader.h
	metadata.h
	mono-config.h
	mono-debug.h
	mono-gc.h
	mono-private-unstable.h
	object.h
	object-forward.h
	opcodes.h
	profiler.h
	profiler-events.h
	reflection.h
	row-indexes.h
	sgen-bridge.h
	threads.h
	tokentype.h
	verify.h)
addprefix(metadata_public_headers ../metadata/ "${metadata_public_headers_base}")
