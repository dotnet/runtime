project (mini)

function (addprefix var prefix list)
  foreach (i ${list})
    set (f ${f} ${prefix}/${i})
  endforeach ()
  set (${var} ${f} PARENT_SCOPE)
endfunction()

include_directories(
  ${PROJECT_BINARY_DIR}/
  ${PROJECT_BINARY_DIR}/../..
  ${PROJECT_BINARY_DIR}/../../mono/eglib
  ${CMAKE_SOURCE_DIR}/
  ${PROJECT_SOURCE_DIR}/../
  ${PROJECT_SOURCE_DIR}/../eglib
  ${PROJECT_SOURCE_DIR}/../sgen)

#
# EGLIB/
#

set (eglib_win32_sources
	eglib-config.hw
	gdate-win32.c gdir-win32.c gfile-win32.c gmisc-win32.c
	gmodule-win32.c gmodule-win32-internals.h gtimer-win32.c gunicode-win32.c)

set (eglib_unix_sources
	gdate-unix.c  gdir-unix.c  gfile-unix.c  gmisc-unix.c
	gmodule-unix.c gtimer-unix.c)

if (HOST_WIN32)
set (eglib_platform_sources ${eglib_win32_sources})
else()
set (eglib_platform_sources ${eglib_unix_sources})
endif()

set (eglib_common_sources
	eglib-remap.h
	sort.frag.h
	glib.h
	garray.c
	gbytearray.c
	gerror.c
	ghashtable.c
	giconv.c
	gmem.c
	gmodule.h
	goutput.c
	gqsort.c
	gstr.c
	gslist.c
	gstring.c
	gptrarray.c
	glist.c
	gqueue.c
	gpath.c
	gshell.c
	gspawn.c
	gfile.c
	gfile-posix.c
	gpattern.c
	gmarkup.c
	gutf8.c
	gunicode.c
	unicode-data.h)

addprefix (eglib_sources ../eglib/ "${eglib_platform_sources};${eglib_common_sources}")

#
# UTILS/
#

set (utils_win32_sources
	os-event-win32.c
	mono-os-wait-win32.c)

set (utils_unix_sources
	os-event-unix.c)

if (HOST_WIN32)
set (utils_platform_sources ${utils_win32_sources})
else()
set (utils_platform_sources ${utils_unix_sources})
endif()

set (utils_common_sources
	mono-md5.c
	mono-sha1.c
	mono-logger.c
	mono-logger-internals.h
	mono-codeman.c
	dlmalloc.h
	dlmalloc.c
	mono-counters.c
	mono-compiler.h
	mono-complex.h
	mono-dl.c
	mono-dl-windows.c
	mono-dl-darwin.c
	mono-dl-posix.c
	mono-dl-wasm.c
	mono-dl.h
	mono-dl-windows-internals.h
	mono-log-windows.c
	mono-log-common.c
	mono-log-posix.c
	mono-log-android.c
	mono-log-darwin.c
	mono-log-flight-recorder.c
	mono-merp.c
	mono-merp.h
	mono-state.h
	mono-state.c
	mono-internal-hash.c
	mono-internal-hash.h
	mono-io-portability.c
	mono-io-portability.h
	monobitset.c
	mono-filemap.c
	mono-math-c.c
	mono-mmap.c
	mono-mmap-windows.c
	mono-mmap.h
	mono-mmap-internals.h
	mono-mmap-windows-internals.h
	mono-os-mutex.h
	mono-os-mutex.c
	mono-flight-recorder.h
	mono-flight-recorder.c
	mono-os-wait.h
	mono-coop-mutex.h
	mono-once.h
	mono-lazy-init.h
	mono-networkinterfaces.c
	mono-networkinterfaces.h
	mono-proclib.c
	mono-proclib-windows.c
	mono-proclib.h
	mono-proclib-windows-internals.h
	mono-publib.c
	mono-jemalloc.c
	mono-string.h
	mono-time.c
	mono-time.h
	strenc.h
	strenc.c
	mono-uri.c
	mono-poll.c
	mono-path.c
	mono-os-semaphore.h
	mono-coop-semaphore.h
	mono-sigcontext.h
	mono-stdlib.c
	mono-property-hash.h
	mono-property-hash.c
	mono-value-hash.h
	mono-value-hash.c
	freebsd-elf_common.h
	freebsd-elf32.h
	freebsd-elf64.h
	freebsd-dwarf.h
	dtrace.h
	gc_wrapper.h
	mono-error.c
	mono-error-internals.h
	monobitset.h
	mono-codeman.h
	mono-counters.h
	mono-digest.h
	mono-error.h
	mono-machine.h
	mono-math.h
	mono-membar.h
	mono-path.h
	mono-poll.h
	mono-uri.h
	mono-stdlib.h
	valgrind.h
	mach-support.h
	memcheck.h
	mono-context.c
	mono-context.h
	mono-stack-unwinding.h
	hazard-pointer.c
	hazard-pointer.h
	lock-free-queue.c
	lock-free-queue.h
	lock-free-alloc.c
	lock-free-alloc.h
	lock-free-array-queue.c
	lock-free-array-queue.h
	lifo-semaphore.c
	lifo-semaphore.h
	mono-linked-list-set.c
	mono-linked-list-set.h
	mono-threads.c
	mono-threads-state-machine.c
	mono-threads-posix.c
	mono-threads-posix-signals.c
	mono-threads-mach.c
	mono-threads-mach-helper.c
	mono-threads-windows.c
	mono-threads-linux.c
	mono-threads-freebsd.c
	mono-threads-netbsd.c
	mono-threads-openbsd.c
	mono-threads-android.c
	mono-threads-haiku.c
	mono-threads-aix.c
	mono-threads-wasm.c
	mono-threads-sunos.c
	mono-threads.h
	mono-threads-debug.h
	mono-threads-api.h
	mono-threads-coop.c
	mono-threads-coop.h
	mono-utility-thread.c
	mono-utility-thread.h
	mono-tls.h
	mono-tls.c
	mono-utils-debug.c
	mono-utils-debug.h
	linux_magic.h
	mono-memory-model.h
	atomic.h
	atomic.c
	mono-hwcap.h
	mono-hwcap.c
	mono-hwcap-vars.h
	bsearch.h
	bsearch.c
	mono-signal-handler.h
	mono-signal-handler.c
	mono-conc-hashtable.h
	mono-conc-hashtable.c
	json.h
	json.c
	networking.c
	networking-posix.c
	networking-fallback.c
	networking-missing.c
	networking-windows.c
	networking.h
	mono-rand.c
	mono-rand-windows.c
	mono-rand.h
	memfuncs.c
	memfuncs.h
	parse.c
	parse.h
	checked-build.c
	checked-build.h
	os-event.h
	refcount.h
	w32api.h
	unlocked.h
	ward.h)

if (TARGET_AMD64)
set (utils_arch_sources
	mach-support-amd64.c)
else()
#message (FATAL_ERROR "")
endif()

if (CROSS_COMPILE)
set (utils_arch_sources "${utils_arch_sources};mono-hwcap-cross.c")
elseif (TARGET_AMD64)
set (utils_arch_sources "${utils_arch_sources};mono-hwcap-x86.c")
else()
  message (FATAL_ERROR "")
endif()

addprefix (utils_sources ../utils/ "${utils_platform_sources};${utils_arch_sources};${utils_common_sources}")

#
# METADATA
#

set (metadata_win32_sources
	console-win32.c
	console-win32-internals.h
	cominterop-win32-internals.h
	w32file-win32.c
	w32file-win32-internals.h
	icall-windows.c
	icall-windows-internals.h
	marshal-windows.c
	marshal-windows-internals.h
	mono-security-windows.c
	mono-security-windows-internals.h
	w32mutex-win32.c
	w32semaphore-win32.c
	w32event-win32.c
	w32process-win32.c
	w32process-win32-internals.h
	w32socket-win32.c
	w32error-win32.c
	w32subset.h)

set (metadata_unix_sources
	console-unix.c
	w32mutex-unix.c
	w32semaphore-unix.c
	w32event-unix.c
	w32process-unix.c
	w32process-unix-internals.h
	w32process-unix-osx.c
	w32process-unix-bsd.c
	w32process-unix-haiku.c
	w32process-unix-default.c
	w32socket-unix.c
	w32file-unix.c
	w32file-unix-glob.c
	w32file-unix-glob.h
	w32error-unix.c)

if (HOST_WIN32)
set (metadata_platform_sources ${metadata_win32_sources})
else()
set (metadata_platform_sources ${metadata_unix_sources})
endif()

set(metadata_common_sources
	appdomain.c
	domain.c
	appdomain-icalls.h
	assembly.c
	assembly-internals.h
	attach.h
	attach.c
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
	console-io.h
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
	w32file.c
	w32file.h
	w32file-internals.h
	filewatcher.c
	filewatcher.h
	gc-internals.h
	icall.c
	icall-internals.h
	icall-def.h
	icall-table.h
	icall-eventpipe.c
	image.c
	image-internals.h
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
	metadata-verify.c
	metadata-internals.h
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
	mono-mlist.c
	mono-mlist.h
	mono-perfcounters.c
	mono-perfcounters.h
	mono-perfcounters-def.h
	mono-ptr-array.h
	mono-route.c
	monitor.h
	normalization-tables.h
	number-formatter.h
	number-ms.h
	object.c
	object-forward.h
	object-internals.h
	opcodes.c
	property-bag.h
	property-bag.c
	w32socket.c
	w32socket.h
	w32socket-internals.h
	w32process.c
	w32process.h
	w32process-internals.h
	profiler.c
	profiler-private.h
	rand.h
	rand.c
	remoting.h
	remoting.c
	runtime.c
	mono-security.c
	security.h
	security-core-clr.c
	security-core-clr.h
	security-manager.c
	security-manager.h
	string-icalls.c
	string-icalls.h
	sysmath.c
	tabledefs.h
	threads.c
	threads-types.h
	threadpool.c
	threadpool.h
	threadpool-worker.h
	threadpool-io.c
	threadpool-io.h
	verify.c
	verify-internals.h
	wrapper-types.h
	dynamic-image-internals.h
	dynamic-stream.c
	dynamic-stream-internals.h
	reflection-cache.h
	custom-attrs-internals.h
	sre-internals.h
	reflection-internals.h
	file-mmap-posix.c
	file-mmap-windows.c
	file-mmap.h
	object-offsets.h
	abi-details.h
	class-abi-details.h
	metadata-cross-helpers.c
	seq-points-data.h
	seq-points-data.c
	handle.c
	handle.h
	w32mutex.h
	w32semaphore.h
	w32event.h
	w32handle-namespace.h
	w32handle-namespace.c
	w32handle.h
	w32handle.c
	w32error.h
	reflection.c
	dynamic-image.c
	sre.c
	sre-encode.c
	sre-save.c
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
	loaded-images-netcore.c
	abi-details.h
	abi.c
	mono-config-dirs.h
	mono-config-dirs.c
	icall-table.h
	icall-table.c
	method-builder-ilgen.h
	method-builder-ilgen.c
	marshal-ilgen.h
	marshal-ilgen.c)

set (metadata_gc_dependent_sources
     gc-stats.c
     gc.c
     monitor.c)

set (metadata_sgen_sources
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
	sgen-client-mono.h
	sgen-mono-ilgen.h
	sgen-mono-ilgen.c)

addprefix (metadata_gc_dependent_real_sources ../metadata "${metadata_gc_dependent_sources}")
addprefix (metadata_sgen_real_sources ../metadata "${metadata_sgen_sources}")
set_source_files_properties (${metadata_sgen_real_sources};${metadata_gc_dependent_real_sources} PROPERTIES COMPILE_DEFINITIONS "HAVE_SGEN_GC")

#
# We localize the usage of MONO_BINDIR etc. to just one source file, thus enabling
# ccache to work even if the value of these defines change.
#
set (bindir "${CMAKE_INSTALL_FULL_BINDIR}")
set (assembliesdir "${CMAKE_INSTALL_FULL_LIBDIR}")
set (confdir "${CMAKE_INSTALL_FULL_SYSCONFDIR}")
set (reloc_libdir "${CMAKE_INSTALL_LIBDIR}")

set_source_files_properties (../metadata/mono-config-dirs.c PROPERTIES COMPILE_DEFINITIONS "MONO_BINDIR=\"${bindir}\";MONO_ASSEMBLIES=\"${assembliesdir}\";MONO_CFG_DIR=\"${confdir}\";MONO_RELOC_LIBDIR=\"../${reloc_libdir}\"")

addprefix (metadata_sources ../metadata/ "${metadata_platform_sources};${metadata_common_sources};${metadata_gc_dependent_sources};${metadata_sgen_sources}")

#
# SGEN
#

set(sgen_sources_base
	gc-internal-agnostic.h
	sgen-alloc.c
	sgen-archdep.h
	sgen-cardtable.c
	sgen-cardtable.h
	sgen-client.h
	sgen-conf.h
	sgen-copy-object.h
	sgen-debug.c
	sgen-descriptor.c
	sgen-descriptor.h
	sgen-fin-weak-hash.c
	sgen-gc.c
	sgen-gc.h
	sgen-gchandles.c
	sgen-gray.c
	sgen-gray.h
	sgen-hash-table.c
	sgen-hash-table.h
	sgen-internal.c
	sgen-layout-stats.c
	sgen-layout-stats.h
	sgen-los.c
	sgen-major-copy-object.h
	sgen-marksweep-drain-gray-stack.h
	sgen-marksweep.c
	sgen-memory-governor.c
	sgen-memory-governor.h
	sgen-minor-copy-object.h
	sgen-minor-scan-object.h
	sgen-nursery-allocator.c
	sgen-pinning-stats.c
	sgen-pinning.c
	sgen-pinning.h
	sgen-pointer-queue.c
	sgen-pointer-queue.h
	sgen-array-list.h
	sgen-array-list.c
	sgen-protocol-def.h
	sgen-protocol.c
	sgen-protocol.h
	sgen-qsort.c
	sgen-qsort.h
	sgen-scan-object.h
	sgen-simple-nursery.c
	sgen-split-nursery.c
	sgen-tagged-pointer.h
	sgen-thread-pool.c
	sgen-thread-pool.h
	sgen-workers.c
	sgen-workers.h)

addprefix (sgen_sources ../sgen/ "${sgen_sources_base}")
set_source_files_properties (${sgen_sources} PROPERTIES COMPILE_DEFINITIONS "HAVE_SGEN_GC")

# ICU
if(HAVE_SYS_ICU)
set (icu_shim_sources_base
	pal_calendarData.c
	pal_casing.c
	pal_collation.c
	pal_idna.c
	pal_locale.c
	pal_localeNumberData.c
	pal_localeStringData.c
	pal_normalization.c
	pal_timeZoneInfo.c
	entrypoints.c
	pal_icushim.c)
addprefix (icu_shim_sources "${ICU_SHIM_PATH}" "${icu_shim_sources_base}")
set_source_files_properties (${icu_shim_sources} PROPERTIES COMPILE_DEFINITIONS OSX_ICU_LIBRARY_PATH="${OSX_ICU_LIBRARY_PATH}")
set_source_files_properties (${icu_shim_sources} PROPERTIES COMPILE_FLAGS "-I${ICU_INCLUDEDIR} -I${CMAKE_SOURCE_DIR}/../libraries/Native/Unix/System.Globalization.Native/ -I${CMAKE_SOURCE_DIR}/../libraries/Native/Unix/Common/ ${ICU_FLAGS}")
set(ICU_LIBS "icucore")
set(ICU_LDFLAGS "-L${ICU_LIBDIR}")
endif()

#
# MINI
#

set (mini_common_sources
	mini.c
	mini-runtime.c
	seq-points.c
	seq-points.h
	ir-emit.h
	method-to-ir.c
	cfgdump.h
	cfgdump.c
	calls.c
	decompose.c
	mini.h
	version.h
	optflags-def.h
	jit-icalls.h
	jit-icalls.c
	trace.c
	trace.h
	patch-info.h
	mini-ops.h
	mini-arch.h
	dominators.c
	cfold.c
	regalloc.h
	helpers.c
	liveness.c
	ssa.c
	abcremoval.c
	abcremoval.h
	local-propagation.c
	driver.c
	debug-mini.c
	linear-scan.c
	aot-compiler.h
	aot-compiler.c
	aot-runtime.c
	aot-runtime-wasm.c
	wasm_m2n_invoke.g.h
	graph.c
	mini-codegen.c
	mini-exceptions.c
	mini-trampolines.c
	branch-opts.c
	mini-generic-sharing.c
	simd-methods.h
	tasklets.c
	tasklets.h
	simd-intrinsics.c
	mini-native-types.c
	mini-unwind.h
	unwind.c
	image-writer.h
	image-writer.c
	dwarfwriter.h
	dwarfwriter.c
	mini-gc.h
	mini-gc.c
	xdebug.c
	mini-llvm.h
	mini-llvm-cpp.h
	llvm-jit.h
	alias-analysis.c
	mini-cross-helpers.c
	arch-stubs.c
	llvm-runtime.h
	llvm-intrinsics.h
	type-checking.c
	lldb.h
	lldb.c
	memory-access.c
	intrinsics.c
	mini-profiler.c
	interp-stubs.c
	aot-runtime.h
	ee.h
	mini-runtime.h
	llvmonly-runtime.h
	llvmonly-runtime.c
	simd-intrinsics-netcore.c
	monovm.h
	monovm.c)

set (debugger_sources
  debugger-engine.h
  debugger-engine.c
  debugger-agent.h
  debugger-agent.c
  debugger-agent-stubs.c
  debugger-state-machine.h
  debugger-state-machine.c)

set (amd64_sources
	mini-amd64.c
	mini-amd64.h
	exceptions-amd64.c
	tramp-amd64.c
	mini-amd64-gsharedvt.c
	mini-amd64-gsharedvt.h
	tramp-amd64-gsharedvt.c
	cpu-amd64.h)

if (TARGET_AMD64)
set (arch_sources ${amd64_sources})
endif()

set (darwin_sources
	mini-darwin.c)

set (windows_sources
	mini-windows.c
	mini-windows.h
	mini-windows-dllmain.c
	mini-windows-dlldac.c)

set (posix_sources
	mini-posix.c)

if (HOST_DARWIN)
set (os_sources "${darwin_sources};${posix_sources}")
endif()

if (NOT DISABLE_INTERPRETER)
set (interp_sources
	interp/interp.h
	interp/interp-internals.h
	interp/interp.c
	interp/interp-intrins.h
	interp/interp-intrins.c
	interp/mintops.h
	interp/mintops.c
	interp/transform.c)
else ()
set (interp_sources
	interp-stubs.c)
endif()

if (ENABLE_LLVM)
set (llvm_sources
	mini-llvm.c
	mini-llvm-cpp.cpp
	llvm-jit.cpp)
endif()

if (ENABLE_LLVM)
set (llvm_runtime_sources
	llvm-runtime.cpp)
elseif (ENABLE_LLVM_RUNTIME)
set (llvm_runtime_sources
	llvm-runtime.cpp)
endif()

set (mini_sources "${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h;main-core.c;${mini_common_sources};${arch_sources};${os_sources};${interp_sources};${llvm_sources};${debugger_sources};${llvm_runtime_sources}")

add_library(monosgen-objects OBJECT "${eglib_sources};${metadata_sources};${utils_sources};${sgen_sources};${icu_shim_sources};${mini_sources}")
add_library (monosgen-static STATIC $<TARGET_OBJECTS:monosgen-objects>)
add_library (monosgen SHARED $<TARGET_OBJECTS:monosgen-objects>)

set_target_properties(monosgen PROPERTIES OUTPUT_NAME monosgen-2.0)
set_target_properties(monosgen PROPERTIES LIBRARY_OUTPUT_DIRECTORY .libs)

# FIXME: Always rebuilds, creates non-deterministic builds
# FIXME: Use the previous format
#string(TIMESTAMP BUILD_DATE)

#add_custom_command (
#  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
#  COMMAND echo "const char *build_date = \"${BUILD_DATE}\";" > ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
#  VERBATIM
#)
add_custom_command (
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
  COMMAND echo "const char *build_date = \"\";" > ${CMAKE_CURRENT_BINARY_DIR}/buildver-sgen.h
  VERBATIM
)

add_custom_command (
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/version.h
  COMMAND ${CMAKE_CURRENT_SOURCE_DIR}/gen-version-h.sh ${CMAKE_CURRENT_SOURCE_DIR}/../..
  VERBATIM
)

add_custom_command (
  OUTPUT ${CMAKE_CURRENT_BINARY_DIR}/cpu-amd64.h
  COMMAND python ${CMAKE_CURRENT_SOURCE_DIR}/genmdesc.py TARGET_AMD64 ${CMAKE_CURRENT_SOURCE_DIR} cpu-amd64.h amd64_desc ${CMAKE_CURRENT_SOURCE_DIR}/cpu-amd64.md
  VERBATIM
)

add_executable (mono-sgen "main-sgen.c")
target_link_libraries (mono-sgen monosgen-static "-framework CoreFoundation" "-framework Foundation" "iconv" ${LLVM_LIBS} ${ICU_LIBS})
target_link_libraries (monosgen "-framework CoreFoundation" "-framework Foundation" "iconv" ${LLVM_LIBS} ${ICU_LIBS})
set_target_properties (mono-sgen monosgen PROPERTIES LINK_FLAGS ${ICU_LDFLAGS})
