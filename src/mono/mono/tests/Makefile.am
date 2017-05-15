SUBDIRS = gc-descriptors . testing_gac

check-local:
	ok=:; \
	$(MAKE) test-jit || ok=false; \
	$(MAKE) test-generic-sharing || ok=false; \
	$(MAKE) test-type-load || ok=false; \
	$(MAKE) test-multi-netmodule || ok=false; \
	$(MAKE) test-cattr-type-load || ok=false; \
	$(MAKE) test-reflection-load-with-context || ok=false; \
	$(MAKE) test-platform || ok=false; \
	$(MAKE) test-console-output || ok=false; \
	$(MAKE) test-env-options || ok=false; \
	$(MAKE) test-unhandled-exception-2 || ok=false; \
	$(MAKE) test-appdomain-unload || ok=false; \
	$(MAKE) test-process-stress || ok=false; \
	$(MAKE) test-pedump || ok=false; \
	$(MAKE) rm-empty-logs || ok=false; \
	$(MAKE) runtest-gac-loading || ok=false; \
	$$ok

check-full:
	ok=; \
	$(MAKE) test-sgen || ok=false; \
	$(MAKE) check-local || ok=false; \
	$$ok

check-parallel:
	ok=; \
	$(MAKE) compile-tests; \
	$(MAKE) check-full || ok=false; \
	$$ok

check-coreclr:
	$(MAKE) -C $(mono_build_root)/acceptance-tests check-coreclr

check-stress:
	$(MAKE) test-stress-sgen

# for backwards compatibility on Wrench
test-wrench: check-parallel

aotcheck: testaot gshared-aot

JITTEST_PROG = $(if $(VALGRIND), valgrind $(VALGRIND_ARGS),) $(if $(SGEN),$(top_builddir)/mono/mini/mono-sgen,$(top_builddir)/mono/mini/mono)

JITTEST_PROG_RUN = MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(JITTEST_PROG)


RUNTIME_ARGS=--config tests-config --optimize=all --debug

CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

with_mono_path = MONO_PATH=$(CLASS)

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper
TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper --aot-path=$(mcs_topdir)/class/lib/build

MKBUNDLE = \
	PKG_CONFIG_PATH=$(top_builddir):$(PKG_CONFIG_PATH) \
	$(RUNTIME) $(CLASS)/mkbundle.exe

if FULL_AOT_TESTS
PROFILE_MCS_FLAGS = -d:MOBILE,MOBILE_LEGACY,FULL_AOT_DESKTOP 
endif

if HYBRID_AOT_TESTS
PROFILE_MCS_FLAGS = -d:MOBILE,MOBILE_LEGACY 
endif

MCS_NO_LIB = $(TOOLS_RUNTIME) $(CSC) -unsafe -debug:portable \
	-noconfig -nologo \
	-nowarn:0162 -nowarn:0168 -nowarn:0219 -nowarn:0414 -nowarn:0618 \
	-nowarn:0169 -nowarn:1690 -nowarn:0649 -nowarn:0612 -nowarn:3021 \
	-nowarn:0197 $(PROFILE_MCS_FLAGS)

MCS = $(MCS_NO_LIB)

ILASM = $(TOOLS_RUNTIME) $(mcs_topdir)/class/lib/build/ilasm.exe

TEST_RUNNER = ./test-runner.exe --runtime $(top_builddir)/runtime/mono-wrapper --mono-path "$(CLASS)"

if FULL_AOT_TESTS
TEST_RUNNER += --runtime-args "$(AOT_RUN_FLAGS)"
endif

if HYBRID_AOT_TESTS
TEST_RUNNER += --runtime-args "$(AOT_RUN_FLAGS)"
endif

if HOST_WIN32
TEST_RUNNER += --config tests-config --runtime $(if $(MONO_EXECUTABLE),$(shell cygpath -w -a $(MONO_EXECUTABLE) | sed 's/\\/\\\\/g'),mono)
else
TEST_RUNNER += --config tests-config --runtime $(if $(MONO_EXECUTABLE),$(MONO_EXECUTABLE),mono)
endif

TEST_RUNNER += $(if $(V), --verbose,)

TESTS_BENCH_SRC=fib.cs random.cs nested-loops.cs ackermann.cs tight-loop.cs sieve.cs

TESTS_STRESS_SRC=	\
	abort-stress-1.cs	\
	abort-stress-2.cs	\
	abort-stress-3.cs	\
	domain-stress.cs	\
	gchandle-stress.cs	\
	monitor-stress.cs	\
	thread-stress.cs	\
	gc-stress.cs		\
	gc-copy-stress.cs	\
	gc-graystack-stress.cs		\
	exit-stress.cs		\
	process-stress.cs	\
	assembly-load-stress.cs

# Disabled until ?mcs is fixed
#	bug-331958.cs
TESTS_CS_SRC=		\
	generic-unloading-sub.2.cs	\
	create-instance.cs	\
	bug-2907.cs		\
	array-init.cs		\
	arraylist.cs		\
	assembly-load-remap.cs	\
	assemblyresolve_event.cs	\
	assemblyresolve_event3.cs	\
	assemblyresolve_event4.cs	\
	checked.cs		\
	char-isnumber.cs	\
	field-layout.cs		\
	pack-layout.cs		\
	pack-bug.cs		\
	hash-table.cs		\
	test-ops.cs		\
	obj.cs			\
	test-dup-mp.cs		\
	string.cs		\
	stringbuilder.cs	\
	switch.cs		\
	outparm.cs		\
	delegate.cs		\
	bitconverter.cs		\
	exception.cs		\
	exception2.cs		\
	exception3.cs		\
	exception4.cs		\
	exception5.cs		\
	exception6.cs		\
	exception7.cs		\
	exception8.cs		\
	exception10.cs		\
	exception11.cs		\
	exception12.cs		\
	exception13.cs		\
	exception14.cs		\
	exception15.cs		\
	exception16.cs		\
	exception17.cs		\
	exception18.cs		\
	typeload-unaligned.cs	\
	struct.cs		\
	valuetype-gettype.cs	\
	typeof-ptr.cs		\
	static-constructor.cs	\
	pinvoke.cs		\
	pinvoke-utf8.cs		\
	pinvoke3.cs		\
	pinvoke11.cs		\
	pinvoke13.cs		\
	pinvoke17.cs		\
	invoke.cs		\
	invoke2.cs		\
	runtime-invoke.cs		\
	invoke-string-ctors.cs		\
	reinit.cs		\
	box.cs			\
	array.cs		\
	enum.cs			\
	enum2.cs		\
	enum-intrins.cs		\
	property.cs		\
	enumcast.cs		\
	assignable-tests.cs	\
	array-cast.cs		\
	array-subtype-attr.cs	\
	cattr-compile.cs	\
	cattr-field.cs		\
	cattr-object.cs		\
	custom-attr.cs		\
	double-cast.cs		\
	newobj-valuetype.cs	\
	arraylist-clone.cs	\
	setenv.cs		\
	vtype.cs		\
	isvaluetype.cs		\
	iface6.cs		\
	iface7.cs		\
	ipaddress.cs		\
	array-vt.cs		\
	interface1.cs		\
	reflection-enum.cs	\
	reflection-prop.cs	\
	reflection4.cs		\
	reflection5.cs		\
	reflection-const-field.cs \
	many-locals.cs		\
	string-compare.cs	\
	test-prime.cs		\
	test-tls.cs		\
	params.cs		\
	reflection.cs		\
	interface.cs		\
	iface.cs		\
	iface2.cs		\
	iface3.cs		\
	iface4.cs		\
	iface-large.cs		\
	virtual-method.cs	\
	intptrcast.cs		\
	indexer.cs		\
	stream.cs		\
	console.cs		\
	shift.cs		\
	jit-int.cs		\
	jit-uint.cs		\
	jit-long.cs		\
	long.cs			\
	jit-ulong.cs		\
	jit-float.cs		\
	pop.cs			\
	time.cs			\
	pointer.cs		\
	hashcode.cs		\
	delegate1.cs		\
	delegate2.cs		\
	delegate3.cs		\
	delegate5.cs		\
	delegate6.cs		\
	delegate7.cs		\
	delegate8.cs		\
	delegate10.cs		\
	delegate11.cs		\
	delegate12.cs		\
	delegate13.cs		\
	largeexp.cs		\
	largeexp2.cs		\
	marshalbyref1.cs	\
	static-ctor.cs		\
	inctest.cs		\
	bound.cs		\
	array-invoke.cs		\
	test-arr.cs		\
	decimal.cs		\
	decimal-array.cs	\
	marshal.cs		\
	marshal1.cs		\
	marshal2.cs		\
	marshal3.cs		\
	marshal5.cs		\
	marshal6.cs		\
	marshal7.cs		\
	marshal8.cs		\
	marshal9.cs		\
	marshalbool.cs		\
	test-byval-in-struct.cs	\
	thread.cs		\
	thread5.cs		\
	thread-static.cs	\
	thread-static-init.cs	\
	context-static.cs	\
	float-pop.cs		\
	interfacecast.cs	\
	array3.cs		\
	classinit.cs		\
	classinit2.cs		\
	classinit3.cs		\
	synchronized.cs		\
	async_read.cs		\
	threadpool.cs		\
	threadpool1.cs		\
	threadpool-exceptions1.cs \
	threadpool-exceptions2.cs \
	threadpool-exceptions3.cs \
	threadpool-exceptions4.cs \
	threadpool-exceptions5.cs \
	threadpool-exceptions6.cs \
	base-definition.cs	\
	bug-27420.cs		\
	bug-46781.cs		\
	bug-42136.cs		\
	bug-59286.cs		\
	bug-70561.cs		\
	bug-78311.cs		\
	bug-78653.cs		\
	bug-78656.cs		\
	bug-77127.cs		\
	bug-323114.cs		\
	bug-Xamarin-5278.cs	\
	interlocked.cs		\
	delegate-async-exit.cs	\
	delegate-delegate-exit.cs	\
	delegate-exit.cs	\
	finalizer-abort.cs	\
	finalizer-exception.cs	\
	finalizer-exit.cs	\
	finalizer-thread.cs	\
	main-exit.cs	\
	main-returns-abort-resetabort.cs	\
	main-returns-background-abort-resetabort.cs	\
	main-returns-background-resetabort.cs	\
	main-returns-background.cs	\
	main-returns-background-change.cs	\
	main-returns.cs		\
	subthread-exit.cs	\
	desweak.cs		\
	exists.cs		\
	handleref.cs	\
	dbnull-missing.cs	\
	test-type-ctor.cs 	\
	soft-float-tests.cs	\
	thread-exit.cs		\
	finalize-parent.cs	\
	interlocked-2.2.cs	\
	pinvoke-2.2.cs 		\
	bug-78431.2.cs 		\
	bug-79684.2.cs 		\
	catch-generics.2.cs	\
	event-get.2.cs		\
	safehandle.2.cs		\
	module-cctor-loader.2.cs	\
	generics-invoke-byref.2.cs	\
	generic-signature-compare.2.cs	\
	generics-sharing.2.cs	\
	shared-generic-methods.2.cs	\
	shared-generic-synchronized.2.cs	\
	generic-inlining.2.cs	\
	generic-initobj.2.cs	\
	generic-delegate.2.cs	\
	generic-sizeof.2.cs	\
	generic-virtual.2.cs	\
	generic-interface-methods.2.cs	\
	generic-array-type.2.cs	\
	generic-method-patching.2.cs	\
	generic-static-methods.2.cs	\
	generic-null-call.2.cs	\
	generic-special.2.cs	\
	generic-special2.2.cs	\
	generic-exceptions.2.cs	\
	generic-virtual2.2.cs	\
	generic-valuetype-interface.2.cs	\
	generic-getgenericarguments.2.cs	\
	generic-synchronized.2.cs	\
	generic-delegate-ctor.2.cs	\
	generic-array-iface-set.2.cs	\
	generic-typedef.2.cs	\
	bug-431413.2.cs	\
	bug-459285.2.cs	\
	generic-virtual-invoke.2.cs	\
	bug-461198.2.cs	\
	generic-sealed-virtual.2.cs	\
	generic-system-arrays.2.cs	\
	generic-stack-traces.2.cs	\
	generic-stack-traces2.2.cs	\
	bug-472600.2.cs	\
	recursive-generics.2.cs	\
	bug-473482.2.cs	\
	bug-473999.2.cs	\
	bug-479763.2.cs	\
	bug-616463.cs	\
	bug-80392.2.cs		\
	bug-82194.2.cs	\
	anonarray.2.cs	\
	ienumerator-interfaces.2.cs	\
	array-enumerator-ifaces.2.cs	\
	generic_type_definition_encoding.2.cs \
	bug-333798.2.cs		\
	bug-348522.2.cs		\
	bug-340662_bug.cs	\
	bug-325283.2.cs	\
	thunks.cs \
	winx64structs.cs \
	nullable_boxing.2.cs	\
	valuetype-equals.cs	\
	custom-modifiers.2.cs	\
	bug-382986.cs	\
	test-inline-call-stack.cs	\
	bug-324535.cs	\
	modules.cs	\
	bug-81673.cs	\
	bug-81691.cs	\
	bug-415577.cs	\
	filter-stack.cs	\
	vararg.cs	\
	vararg2.cs	\
	bug-461867.cs	\
	bug-461941.cs	\
	bug-461261.cs	\
	bug-400716.cs	\
	bug-459094.cs	\
	bug-467456.cs	\
	bug-508538.cs	\
	bug-472692.2.cs		\
	gchandles.cs	\
	interlocked-3.cs	\
	interlocked-4.2.cs	\
	w32message.cs	\
	gc-altstack.cs	\
	large-gc-bitmap.cs	\
	bug-561239.cs	\
	bug-562150.cs	\
	bug-599469.cs	\
	monitor-resurrection.cs	\
	monitor-wait-abort.cs	\
	monitor-abort.cs	\
	bug-666008.cs	\
	bug-685908.cs	\
	sgen-long-vtype.cs	\
	delegate-invoke.cs	\
	bug-696593.cs	\
	bug-705140.cs	\
	bug-1147.cs	\
	mono-path.cs	\
	bug-bxc-795.cs	\
	bug-3903.cs	\
	async-with-cb-throws.cs	\
	bug-6148.cs	\
	bug-10127.cs	\
	bug-18026.cs	\
	allow-synchronous-major.cs	\
	block_guard_restore_aligment_on_exit.cs	\
	thread_static_gc_layout.cs \
	sleep.cs \
	bug-27147.cs	\
	bug-30085.cs	\
	bug-17537.cs	\
	pinvoke_ppcc.cs	\
	pinvoke_ppcs.cs	\
	pinvoke_ppci.cs	\
	pinvoke_ppcf.cs	\
	pinvoke_ppcd.cs	\
	bug-29585.cs	\
	priority.cs	\
	abort-cctor.cs	\
	thread-native-exit.cs \
	reference-loader.cs \
	thread-suspend-suspended.cs \
	thread-suspend-selfsuspended.cs \
	remoting4.cs	\
	remoting1.cs	\
	remoting2.cs	\
	remoting3.cs	\
	remoting5.cs	\
	appdomain.cs	\
	appdomain-client.cs	\
	appdomain-unload.cs	\
	appdomain-async-invoke.cs	\
	appdomain-thread-abort.cs	\
	appdomain1.cs	\
	appdomain2.cs	\
	appdomain-exit.cs	\
	appdomain-serialize-exception.cs \
	assemblyresolve_event2.2.cs	\
	appdomain-unload-callback.cs	\
	appdomain-unload-doesnot-raise-pending-events.cs	\
	unload-appdomain-on-shutdown.cs	\
	bug-47295.cs	\
	loader.cs	\
	pinvoke2.cs	\
	generic-type-builder.2.cs	\
	dynamic-generic-size.cs	\
	cominterop.cs	\
	dynamic-method-access.2.cs	\
	dynamic-method-finalize.2.cs	\
	dynamic-method-stack-traces.cs	\
	generic_type_definition.2.cs	\
	bug-333798-tb.2.cs	\
	bug-335131.2.cs	\
	bug-322722_patch_bx.2.cs	\
	bug-322722_dyn_method_throw.2.cs	\
	bug-389886-2.cs	\
	bug-349190.2.cs	\
	bug-389886-sre-generic-interface-instances.cs	\
	bug-462592.cs	\
	bug-575941.cs	\
	bug-389886-3.cs	\
	constant-division.cs	\
	dynamic-method-resurrection.cs	\
	bug-80307.cs	\
	assembly_append_ordering.cs	\
	bug-544446.cs	\
	bug-36848.cs	\
	generic-marshalbyref.2.cs	\
	stackframes-async.2.cs	\
	transparentproxy.cs	\
	bug-48015.cs	\
	delegate9.cs	\
	marshal-valuetypes.cs	\
	xdomain-threads.cs	\
	monitor.cs	\
	generic-xdomain.2.cs	\
	threadpool-exceptions7.cs	\
	cross-domain.cs	\
	generic-unloading.2.cs	\
	namedmutex-destroy-race.cs	\
	thread6.cs	\
	appdomain-threadpool-unload.cs	\
	process-unref-race.cs	\
	bug-46661.cs	\
	w32message.cs \
	runtime-invoke.gen.cs \
	imt_big_iface_test.cs

if AMD64
TESTS_CS_SRC += async-exc-compilation.cs finally_guard.cs finally_block_ending_in_dead_bb.cs
endif

if X86
TESTS_CS_SRC += async-exc-compilation.cs finally_guard.cs finally_block_ending_in_dead_bb.cs
endif

TESTS_IL_SRC=			\
	field-access.il		\
	method-access.il	\
	ldftn-access.il		\
	cpblkTest.il		\
	vbinterface.il		\
	calliTest.il		\
	calliGenericTest.il		\
	ckfiniteTest.il		\
	fault-handler.il		\
	locallocTest.il		\
	initblkTest.il		\
	qt-instance.il		\
	vararg.il		\
	bug-29859.il		\
	bug-78549.il		\
	static-fields-nonconst.il	\
	reload-at-bb-end.il	\
	test-enum-indstoreil.il	\
	filter-bug.il		\
	even-odd.il		\
	bug-82022.il	\
	vt-sync-method.il	\
	enum_types.il	\
	invalid-token.il	\
	call_missing_method.il	\
	call_missing_class.il	\
	ldfld_missing_field.il	\
	ldfld_missing_class.il	\
	find-method.2.il	\
	bug-79215.2.il	\
	bug-79956.2.il	\
	bug-327438.2.il \
	bug-387274.2.il	\
	bug-426309.2.il	\
	ldtoken_with_byref_typespec.2.il \
	resolve_method_bug.2.il \
	resolve_field_bug.2.il \
	resolve_type_bug.2.il	\
	generics-sharing-other-exc.2.il	\
	generic-ldobj.2.il	\
	generic-mkrefany.2.il	\
	generic-refanyval.2.il	\
	generic-ldtoken.2.il	\
	generic-ldtoken-method.2.il	\
	generic-ldtoken-field.2.il	\
	generic-tailcall.2.il	\
	generic-tailcall2.2.il	\
	generic-array-exc.2.il	\
	generic-valuetype-newobj2.2.il	\
	generic-valuetype-newobj.2.il	\
	generic-constrained.2.il	\
	generic-type-load-exception.2.il	\
	bug-81466.il	\
	bug457574.il	\
	bug445361.il	\
	bug-463303.il	\
	bug469742.2.il	\
	bug-528055.il	\
	array_load_exception.il \
	bug-481403.il	\
	interface-with-static-method.il	\
	bug-633291.il	\
	delegate-with-null-target.il	\
	bug-318677.il	\
	gsharing-valuetype-layout.il	\
	invalid_generic_instantiation.il \
	bug-45841-fpstack-exceptions.il \
	instance_tailrec.il

TESTS_GSHARED_SRC = \
	generics-sharing.2.cs	\
	shared-generic-methods.2.cs	\
	shared-generic-synchronized.2.cs	\
	generic-initobj.2.cs		\
	generics-sharing-other-exc.2.cs	\
	generic-box.2.cs		\
	generic-unbox.2.cs	\
	generic-delegate.2.cs	\
	generic-sizeof.2.cs	\
	generic-ldobj.2.cs	\
	generic-mkrefany.2.cs			\
	generic-refanyval.2.cs	\
	generic-ldtoken.2.cs			\
	generic-ldtoken-method.2.cs	\
	generic-ldtoken-field.2.cs	\
	generic-virtual.2.cs	\
	generic-tailcall.2.cs			\
	generic-interface-methods.2.cs	\
	generic-array-type.2.cs	\
	generic-method-patching.2.cs	\
	generic-static-methods.2.cs	\
	generic-null-call.2.cs	\
	generic-tailcall2.2.cs			\
	generic-array-exc.2.cs	\
	generic-special.2.cs			\
	generic-special2.2.cs	\
	generic-exceptions.2.cs	\
	generic-delegate2.2.cs		\
	generic-virtual2.2.cs	\
	generic-valuetype-interface.2.cs	\
	generic-valuetype-newobj.2.cs	\
	generic-valuetype-newobj2.2.cs	\
	generic-getgenericarguments.2.cs	\
	generic-synchronized.2.cs	\
	generic-delegate-ctor.2.cs		\
	generic-constrained.2.cs	\
	bug-431413.2.cs			\
	generic-virtual-invoke.2.cs	\
	generic-typedef.2.cs		\
	generic-marshalbyref.2.cs	\
	bug-459285.2.cs	\
	bug-461198.2.cs	\
	generic-sealed-virtual.2.cs	\
	generic-system-arrays.2.cs	\
	generic-stack-traces.2.cs	\
	generic-stack-traces2.2.cs		\
	bug-472600.2.cs	\
	bug-473482.2.cs	\
	bug-473999.2.cs		\
	bug-479763.2.cs				\
	generic-type-load-exception.2.cs	\
	bug-616463.cs	\
	bug-1147.cs	\
	generic-type-builder.2.cs

if AMD64
# #651684
PLATFORM_DISABLED_TESTS = finally_guard.exe

if HOST_WIN32
PLATFORM_DISABLED_TESTS += w32message.exe
endif

endif

if X86

if HOST_WIN32
PLATFORM_DISABLED_TESTS=async-exc-compilation.exe finally_guard.exe finally_block_ending_in_dead_bb.exe \
	bug-18026.exe monitor.exe threadpool-exceptions5.exe process-unref-race.exe w32message.exe \
	unhandled-exception-1.exe unhandled-exception-2.exe unhandled-exception-3.exe unhandled-exception-4.exe \
	unhandled-exception-5.exe unhandled-exception-6.exe unhandled-exception-7.exe unhandled-exception-8.exe
endif

endif

if POWERPC
# bug #71274
PLATFORM_DISABLED_TESTS=finalizer-abort.exe finalizer-exception.exe finalizer-exit.exe
endif

if POWERPC64
# FIXME: These tests hang/fail for unknown reasons
PLATFORM_DISABLED_TESTS=monitor.exe threadpool-exceptions5.exe appdomain-thread-abort.exe appdomain-unload.exe \
	pinvoke2.exe pinvoke3.exe pinvoke11.exe threadpool-exceptions7.exe winx64structs.exe bug-10127.exe pinvoke_ppcc.exe \
	pinvoke_ppcs.exe pinvoke_ppci.exe pinvoke_ppcf.exe pinvoke_ppcd.exe abort-cctor.exe load-exceptions.exe \
	sgen-domain-unload-2.exe sgen-weakref-stress.exe sgen-cementing-stress.exe sgen-new-threads-dont-join-stw.exe \
	sgen-new-threads-dont-join-stw-2.exe sgen-new-threads-collect.exe sgen-bridge.exe
endif

if ARM
PLATFORM_DISABLED_TESTS=filter-stack.exe
endif

if MIPS
# monitor.exe is racy
PLATFORM_DISABLED_TESTS=filter-stack.exe monitor.exe
endif

if S390X
PLATFORM_DISABLED_TESTS=dynamic-method-resurrection.exe
#PLATFORM_DISABLED_TESTS=dynamic-method-resurrection.exe exception17.exe

PLATFORM_DISABLED_TESTS += \
	sgen-toggleref.exe \
	sgen-bridge.exe \
	sgen-bridge-major-fragmentation.exe \
	sgen-bridge-xref.exe
endif

if NACL_CODEGEN
# Tests that use Thread.Abort()
PLATFORM_DISABLED_TESTS= abort-stress-1.exe \
                         abort-stress-2.exe \
                         abort-stress-3.exe \
                         appdomain-thread-abort.exe \
                         async-exc-compilation.exe \
                         bug-561239.exe \
                         bug-70561.exe \
                         finalizer-abort.exe \
                         finally_guard.exe \
                         finally_block_ending_in_dead_bb.exe \
                         main-returns-abort-resetabort.exe \
                         main-returns-background-abort-resetabort.exe \
                         thread6.exe \
                         threadpool-exceptions5.exe \
                         threadpool-exceptions6.exe

PLATFORM_DISABLED_TESTS+= w32message.exe

# Tests that rely on AppDomain.Unload
PLATFORM_DISABLED_TESTS+= appdomain-async-invoke.exe \
                          appdomain-exit.exe \
                          appdomain-unload-callback.exe \
                          appdomain-unload.exe \
                          domain-stress.exe \
                          generic-unloading.2.exe \
                          monitor.exe \
                          remoting4.exe \
                          threadpool-exceptions7.exe \
                          xdomain-threads.exe

# pinvoke2 attaches a thread to the runtime, but
# doesn't 'unattach' it and it hangs in GC on exit
PLATFORM_DISABLED_TESTS+= pinvoke2.exe

# Tests that currently hang waiting for non-main threads
# to exit in NaCl, need to investigate.  Most are AppDomain
# creation and Delegate tests.
PLATFORM_DISABLED_TESTS+= appdomain1.exe \
                          delegate9.exe \
                          marshal-valuetypes.exe \
                          cross-domain.exe \
                          stackframes-async.2.exe \
                          generic-marshalbyref.2.exe \
                          generic-xdomain.2.exe \
                          bug-415577.exe

# Tests that fail trying to write files (appdomain create mostly)
PLATFORM_DISABLED_TESTS+= bug-335131.2.exe \
                          bug-349190.2.exe \
                          bug-80307.exe \
                          bug-462592.exe

# FIXME: don't know why delegate2.exe fails, it shouldn't
PLATFORM_DISABLED_TESTS+= delegate2.exe

# These tests newly fail with the latest revision. pinvoke3 fails because
# of a thread attach, the others have not been investigated.  TODO revisit.
PLATFORM_DISABLED_TESTS+= pinvoke3.exe \
                          async_read.exe \
                          async-with-cb-throws.exe \
                          appdomain-unload-doesnot-raise-pending-events.exe \
                          gsharing-valuetype-layout.exe

if X86
# FIXME: There are problems with async callbacks and results on NaCl 32-bit
PLATFORM_DISABLED_TESTS+= delegate1.exe \
                          delegate3.exe \
                          delegate5.exe \
                          delegate8.exe \
                          threadpool.exe \
                          threadpool1.exe \
                          threadpool-exceptions3.exe \
                          bug-323114.exe \
                          delegate-exit.exe \
                          bug-80392.2.exe

# FIXME: These tests hang/fail for unknown reasons, deal with exiting
PLATFORM_DISABLED_TESTS+= main-returns-background-resetabort.exe \
                          main-returns-background.exe \
                          main-returns-background-change.exe
endif

endif

if ENABLE_COOP
COOP_DISABLED_TESTS= thunks.exe
else
COOP_DISABLED_TESTS= 
endif

PROFILE_DISABLED_TESTS=

if FULL_AOT_TESTS
# Tests which rely on TypeLoadExceptions
# In full-aot mode, these cause the relevant methods to be not AOTed.
PROFILE_DISABLED_TESTS += \
	typeload-unaligned.exe \
	field-access.exe \
	invalid_generic_instantiation.exe \
	bug-481403.exe \
	array_ldelema.exe \
	array_load_exception.exe \
	bug445361.exe \
	generic-type-load-exception.2.exe \
	invalid-token.exe \
	call_missing_method.exe \
	call_missing_class.exe \
	ldfld_missing_field.exe \
	ldfld_missing_class.exe \
	vt-sync-method.exe \
	resolve_method_bug.2.exe \
	resolve_field_bug.2.exe \
	resolve_type_bug.2.exe

# Tests which rely on remoting
PROFILE_DISABLED_TESTS += \
	context-static.exe \
	bug-415577.exe \
	generic-marshalbyref.2.exe \
	unhandled-exception-7.exe

# Tests which use unsupported pinvoke+full aot
# functionality
PROFILE_DISABLED_TESTS += \
	marshal.exe \
	marshal2.exe \
	marshal6.exe \
	marshal7.exe \
	marshal8.exe \
	pinvoke-2.2.exe \
	pinvoke3.exe \
	thunks.exe

# Tests which load assemblies which are not
# in the testing_aot_full profile
PROFILE_DISABLED_TESTS += \
	assembly-load-remap.exe

# Test which needs remoting support
PROFILE_DISABLED_TESTS += \
	remoting4.exe	\
	remoting1.exe	\
	remoting2.exe	\
	remoting3.exe	\
	remoting5.exe

# Tests which needs AppDomain support
PROFILE_DISABLED_TESTS += \
	appdomain.exe	\
	appdomain-client.exe	\
	appdomain-unload.exe	\
	appdomain-async-invoke.exe	\
	appdomain-thread-abort.exe	\
	appdomain1.exe	\
	appdomain2.exe	\
	appdomain-exit.exe	\
	appdomain-unload-callback.exe	\
	appdomain-unload-doesnot-raise-pending-events.exe	\
	unload-appdomain-on-shutdown.exe	\
	assemblyresolve_event2.2.exe	\
	bug-544446.exe	\
	bug-36848.exe	\
	generic-marshalbyref.2.exe	\
	stackframes-async.2.exe	\
	transparentproxy.exe	\
	bug-48015.exe	\
	delegate9.exe	\
	marshal-valuetypes.exe	\
	xdomain-threads.exe	\
	monitor.exe	\
	generic-xdomain.2.exe	\
	threadpool-exceptions7.exe	\
	cross-domain.exe	\
	generic-unloading.2.exe	\
	appdomain-threadpool-unload.exe

# Tests which needs System.Reflection.Emit support
PROFILE_DISABLED_TESTS += \
	bug-47295.exe	\
	loader.exe	\
	pinvoke2.exe	\
	generic-type-builder.2.exe	\
	dynamic-generic-size.exe	\
	cominterop.exe	\
	dynamic-method-access.2.exe	\
	dynamic-method-finalize.2.exe	\
	dynamic-method-stack-traces.exe	\
	generic_type_definition.2.exe	\
	bug-333798-tb.2.exe	\
	bug-335131.2.exe	\
	bug-322722_patch_bx.2.exe	\
	bug-322722_dyn_method_throw.2.exe	\
	bug-389886-2.exe	\
	bug-349190.2.exe	\
	bug-389886-sre-generic-interface-instances.exe	\
	bug-462592.exe	\
	bug-575941.exe	\
	bug-389886-3.exe	\
	constant-division.exe	\
	dynamic-method-resurrection.exe	\
	assembly_append_ordering.exe

# Test which needs System.Web support
PROFILE_DISABLED_TESTS += \
	bug-80307.exe

# Tests which needs named Mutex support
PROFILE_DISABLED_TESTS += \
	namedmutex-destroy-race.exe

# ThreadAbortException doesn't have necessary field for this test
PROFILE_DISABLED_TESTS += \
	thread6.exe

# can't AOT the TestingReferenceAssembly.dll which is a dependency
# of reference-loader.exe because it contains the [ReferenceAssemblyAttribute]
# and the runtime errors out with "File does not contain a valid CIL image."
PROFILE_DISABLED_TESTS += \
	reference-loader.exe

# constraints-load.il: 
# Failed to load method 0x6000007 from '..../mono/tests/constraints-load.exe' due to 
# Could not resolve type with token 01000002 assembly:mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089 type:System.BrokenIComparable`1 member:<none>.
PROFILE_DISABLED_TESTS += \
	constraints-load.exe \
	bug-515884.exe

PROFILE_DISABLED_TESTS += \
	generic-type-builder.2.exe

PROFILE_DISABLED_TESTS += \
	sgen-domain-unload.exe	\
	sgen-domain-unload-2.exe

PROFILE_DISABLED_TESTS += \
	appdomain-loader.exe
endif

if HYBRID_AOT_TESTS
PROFILE_DISABLED_TESTS += \
	bug-80307.exe \
	namedmutex-destroy-race.exe
endif

AOT_DISABLED_TESTS= \
	constraints-load.exe

CI_DISABLED_TESTS = \
	main-returns-background-resetabort.exe \
	main-returns-background-abort-resetabort.exe	\
	assemblyresolve_event3.exe \
	delegate2.exe	\
	finally_guard.exe \
	generic-xdomain.2.exe

# failing tests which we temporarily disable for PRs
# so they don't interfere with other people's work
CI_PR_DISABLED_TESTS = \
	appdomain-threadpool-unload.exe	\
	appdomain-thread-abort.exe

# appdomain-threadpool-unload.exe creates 100 appdomains, takes too long with llvm
LLVM_DISABLED_TESTS = \
	appdomain-threadpool-unload.exe

LLVM = $(filter --llvm, $(MONO_ENV_OPTIONS))

# The two finalizer tests only work under sgen
# gc-altstack.exe fails under boehm because it has no support for altstack
# bug-459094.exe creates an extremely deep directory tree
# delegate-invoke.exe depends on 929c6bc9b6d76a273f251e6f5dfacac36e9c38bd which was
# reverted.
# bug-Xamarin-5278.exe got broken by 5d26590e79da139a284459299aee95c25f4cd835
# bug-45841-fpstack-exceptions.exe: https://bugzilla.xamarin.com/show_bug.cgi?id=47053
# appdomain-thread-abort.exe: https://bugzilla.xamarin.com/show_bug.cgi?id=47054
KNOWN_FAILING_TESTS = \
	delegate-async-exception.exe	\
	bug-348522.2.exe	\
	bug-459094.exe \
	delegate-invoke.exe \
	bug-Xamarin-5278.exe \
	bug-45841-fpstack-exceptions.exe \
	appdomain-thread-abort.exe

DISABLED_TESTS = \
	$(KNOWN_FAILING_TESTS) \
	$(PLATFORM_DISABLED_TESTS) \
	$(EXTRA_DISABLED_TESTS) \
	$(COOP_DISABLED_TESTS) \
	$(PROFILE_DISABLED_TESTS) \
	$(if $(AOT),$(AOT_DISABLED_TESTS)) \
	$(if $(CI),$(CI_DISABLED_TESTS)) \
	$(if $(CI_PR),$CI_PR_DISABLED_TESTS) \
	$(if $(LLVM),$(LLVM_DISABLED_TESTS))

INTERP_DISABLED_TESTS = \
	$(CI_PR_DISABLED_TESTS) \
	$(CI_DISABLED_TESTS) \
	$(KNOWN_FAILING_TESTS) \
	abort-cctor.exe \
	appdomain-async-invoke.exe \
	appdomain-exit.exe \
	appdomain-serialize-exception.exe \
	appdomain-unload-callback.exe \
	appdomain-unload-doesnot-raise-pending-events.exe \
	appdomain-unload.exe \
	appdomain.exe \
	appdomain1.exe \
	appdomain2.exe \
	array_load_exception.exe \
	assembly_append_ordering.exe \
	assemblyresolve_event4.exe \
	async-exc-compilation.exe \
	async-with-cb-throws.exe \
	async_read.exe \
	bug-18026.exe \
	bug-2907.exe \
	bug-323114.exe \
	bug-327438.2.exe \
	bug-335131.2.exe \
	bug-415577.exe \
	bug-461867.exe \
	bug-461941.exe \
	bug-46661.exe \
	bug-47295.exe \
	bug-48015.exe \
	bug-544446.exe \
	bug-685908.exe \
	bug-80307.exe \
	bug-80392.2.exe \
	bug-81673.exe \
	bug-82022.exe \
	bug445361.exe \
	call_missing_class.exe \
	call_missing_method.exe \
	calliGenericTest.exe \
	cominterop.exe \
	cross-domain.exe \
	delegate-with-null-target.exe \
	delegate9.exe \
	dynamic-method-stack-traces.exe \
	even-odd.exe \
	exception18.exe \
	field-access.exe \
	finally_block_ending_in_dead_bb.exe \
	generic-marshalbyref.2.exe \
	generic-mkrefany.2.exe \
	generic-refanyval.2.exe \
	generic-stack-traces2.2.exe \
	generic-type-load-exception.2.exe \
	generic-unloading.2.exe \
	generics-invoke-byref.2.exe \
	generics-sharing.2.exe \
	handleref.exe \
	invalid-token.exe \
	invalid_generic_instantiation.exe \
	ldfld_missing_class.exe \
	ldfld_missing_field.exe \
	ldftn-access.exe \
	main-returns-background-change.exe \
	marshal-valuetypes.exe \
	marshal.exe \
	marshal2.exe \
	marshal8.exe \
	marshal9.exe \
	marshalbool.exe \
	method-access.exe \
	monitor.exe \
	nullable_boxing.2.exe \
	pinvoke-2.2.exe \
	pinvoke-utf8.exe \
	pinvoke.exe \
	pinvoke11.exe \
	pinvoke2.exe \
	pinvoke3.exe \
	pinvoke_ppcc.exe \
	pinvoke_ppcd.exe \
	pinvoke_ppcf.exe \
	pinvoke_ppci.exe \
	pinvoke_ppcs.exe \
	process-unref-race.exe \
	reload-at-bb-end.exe \
	remoting1.exe \
	remoting2.exe \
	remoting3.exe \
	remoting4.exe \
	remoting5.exe \
	safehandle.2.exe \
	stackframes-async.2.exe \
	static-constructor.exe \
	test-inline-call-stack.exe \
	test-type-ctor.exe \
	thread6.exe \
	threadpool-exceptions1.exe \
	threadpool-exceptions2.exe \
	threadpool-exceptions3.exe \
	threadpool-exceptions4.exe \
	threadpool-exceptions5.exe \
	threadpool-exceptions6.exe \
	threadpool-exceptions7.exe \
	threadpool.exe \
	threadpool1.exe \
	thunks.exe \
	transparentproxy.exe \
	typeload-unaligned.exe \
	unload-appdomain-on-shutdown.exe \
	vararg.exe \
	vararg2.exe \
	vt-sync-method.exe \
	winx64structs.exe \
	xdomain-threads.exe

TESTS_CS=$(filter-out $(DISABLED_TESTS),$(TESTS_CS_SRC:.cs=.exe))
TESTS_IL=$(filter-out $(DISABLED_TESTS),$(TESTS_IL_SRC:.il=.exe))
TESTS_BENCH=$(filter-out $(DISABLED_TESTS),$(TESTS_BENCH_SRC:.cs=.exe))
TESTS_STRESS=$(filter-out $(DISABLED_TESTS),$(TESTS_STRESS_SRC:.cs=.exe))
TESTS_GSHARED=$(filter-out $(DISABLED_TESTS),$(TESTS_GSHARED_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_CS=$(TESTS_CS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_IL=$(TESTS_IL:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_BENCH=$(TESTS_BENCH:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_STRESS=$(TESTS_STRESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_GSHARED=$(TESTS_GSHARED:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_CS=$(TESTS_CS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_IL=$(TESTS_IL:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_BENCH=$(TESTS_BENCH:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_STRESS=$(TESTS_STRESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_GSHARED=$(TESTS_GSHARED:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

EXTRA_DIST=test-driver test-runner.cs $(TESTS_CS_SRC) $(TESTS_IL_SRC) \
	$(TESTS_BENCH_SRC) $(TESTS_STRESS_SRC) stress-runner.pl

%.exe: %.il
	$(ILASM) -out:$@ $<

if !FULL_AOT_TESTS
if !HYBRID_AOT_TESTS
TEST_DRIVER_HARD_KILL_FEATURE=-r:$(CLASS)/Mono.Posix.dll
endif
endif

if FULL_AOT_TESTS
TEST_DRIVER_DEPEND=TestDriver.dll$(PLATFORM_AOT_SUFFIX)
else
if HYBRID_AOT_TESTS
TEST_DRIVER_DEPEND=TestDriver.dll$(PLATFORM_AOT_SUFFIX)
else
TEST_DRIVER_DEPEND=TestDriver.dll
endif
endif

%.exe: %.cs $(TEST_DRIVER_DEPEND)
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -r:TestDriver.dll $(TEST_DRIVER_HARD_KILL_FEATURE) -out:$@ $<

# N.B. test-runner.exe references the TOOLS_RUNTIME versions of the framework assemblies
test-runner.exe: test-runner.cs $(TEST_DRIVER_DEPEND)
	$(MCS) -r:System.dll -r:System.Xml.dll -r:System.Core.dll -r:TestDriver.dll $(TEST_DRIVER_HARD_KILL_FEATURE) -out:$@ $<

%.dll: %.cs
	$(MCS) -r:$(CLASS)/System.dll -target:library -out:$@ $<
%.dll: %.il
	$(ILASM) /dll /output:$@ $<

reference-loader.exe: reference-loader.cs TestingReferenceAssembly.dll TestingReferenceReferenceAssembly.dll $(TEST_DRIVER_DEPEND)
	$(MCS) -r:$(CLASS)/System.dll -r:TestDriver.dll -r:TestingReferenceAssembly.dll -r:TestingReferenceReferenceAssembly.dll $(TEST_DRIVER_HARD_KILL_FEATURE) -out:$@ $(srcdir)/reference-loader.cs

TestingReferenceAssembly.dll: TestingReferenceAssembly.cs
	$(MCS) -target:library -out:$@ $<

TestingReferenceReferenceAssembly.dll: TestingReferenceReferenceAssembly.cs TestingReferenceAssembly.dll
	$(MCS) -r:TestingReferenceAssembly.dll -target:library -out:$@ $<

%.exe$(PLATFORM_AOT_SUFFIX): %.exe 
	$(RUNTIME) $(AOT_BUILD_FLAGS) $<

%.dll$(PLATFORM_AOT_SUFFIX): %.dll 
	$(RUNTIME) $(AOT_BUILD_FLAGS) $<

# mkbundle works on ppc, but the pkg-config POC doesn't when run with make test
if POWERPC
test-platform:
else
# Can't use mkbundle on win32 since there is no static build there
# Can't run test-unhandled-exception on Windows because of all the debug popups...
if HOST_WIN32
test-platform:
else
# mkbundle uses the installed mono-2.pc so it won't work if there is no system mono
#test-platform:	testbundle test-iomap-regression
test-platform:	test-iomap-regression
endif
endif

if X86
if HOST_WIN32
else
test-platform: test-eglib-remap
endif
endif

# Target to precompile the test executables
tests: compile-tests

#
# Test that no symbols are missed in eglib-remap.h
#
OK_G_SYMBOLS='g_list\|g_slist\|g_concat_dir_and_file\|g_Ctoc'
if NACL_CODEGEN
test-eglib-remap:
else
test-eglib-remap:
	@echo "Testing eglib remap..."
	@if which nm > /dev/null; then if nm $(top_builddir)/mono/mini/mono | grep -v $(OK_G_SYMBOLS) | grep 't g_'; then exit 1; else exit 0; fi; fi
endif

test-env-options:
	MONO_ENV_OPTIONS="--version" $(RUNTIME) array-init.exe | grep -q Architecture:

# Precompile the test assemblies in parallel
compile-tests:
	$(MAKE) -j4 $(TESTS_CS) $(TESTS_IL) $(TESTS_BENCH) $(TESTS_STRESS) $(TESTS_GSHARED) $(TESTSAOT_CS) $(TESTSAOT_IL) $(TESTSAOT_BENCH) $(TESTSAOT_STRESS) $(TESTSAOT_GSHARED) compile-gac-loading

# Remove empty .stdout and .stderr files for wrench
rm-empty-logs:
	@echo "Removing empty logs..."
	find . '(' -name "*.stdout" -o -name "*.stderr" ')' -size 0 -exec rm {} \;

TestDriver.dll:
	$(MCS) -target:library -out:$@ $(srcdir)/../mini/TestDriver.cs $(srcdir)/../mini/TestHelpers.cs

runtest: compile-tests
	@failed=0; \
	passed=0; \
	failed_tests="";\
	if [ "x$$V" = "x1" ]; then dump_action="dump-output"; else dump_action="no-dump"; fi; \
	rm -f testlist testlist.sorted; \
	for i in $(TESTS_CS) $(TESTS_IL) $(TESTS_BENCH); do echo $${i} >> testlist; sort testlist > testlist.sorted; done; \
	for i in `cat testlist.sorted`; do \
		rm -f $${i}.so; \
		$(with_mono_path) $(JITTEST_PROG_RUN) --aot --debug $${i} > $${i}.aotlog 2>&1 || exit 1; \
		if $(srcdir)/test-driver '$(with_mono_path) $(JITTEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" "$${dump_action}" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	rm -f testlist testlist.sorted; \
	if [ $${failed} != 0 ]; then \
	  echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do \
	    echo $${i}; \
	    if [ "x$$V" = "x1" ]; then \
	      cat $${i}.stdout; \
	      cat $${i}.stderr; \
	    fi; \
	  done; \
	  exit 1; \
	fi

runtest-managed: test-runner.exe compile-tests
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j a --testsuite-name "runtime" --timeout 300 --disabled "$(DISABLED_TESTS)" $(TESTS_CS) $(TESTS_IL) $(TESTS_BENCH)

runtest-managed-serial: test-runner.exe compile-tests
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j 1 --testsuite-name "runtime" --disabled "$(DISABLED_TESTS)" $(TESTS_CS) $(TESTS_IL) $(TESTS_BENCH)

test-jit:
	@if test x$(M) != x0; then $(MAKE) runtest-managed; else $(MAKE) runtest; fi

testaot:
	@$(MAKE) AOT=1 runtest

testtrace:
	@$(MAKE) RUNTIME_ARGS="$${RUNTIME_ARGS} --trace" runtest

testinterp: test-runner.exe compile-tests
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j a --runtime-args "--interpreter" --testsuite-name "runtime-interp" --timeout 300 --disabled "$(INTERP_DISABLED_TESTS)" $(TESTS_CS) $(TESTS_IL) $(TESTS_BENCH)

testjitspeed: $(JITTEST_PROG) compile-tests
	for i in $(TESTS_BENCH); do	\
		echo $$i;	\
		time $(JITTEST_PROG) $$i;	\
	done

test-iomap-regression: exists.exe
	@echo "Testing exists.exe..."
	@MONO_IOMAP=all $(RUNTIME) exists.exe

stresstest: compile-tests
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTS_STRESS); do	\
		if $(srcdir)/stress-runner.pl $$i ../mini/mono $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $${failed} != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testbundle: console.exe
	@echo "Testing mkbundle..."
	@$(MKBUNDLE) --static console.exe > mkbundle.stdout
	@$(with_mono_path) MONO_CFG_DIR=$(mono_cfg_dir) ./a.out >> mkbundle.stdout
	@- rm -rf a.out

EXTRA_DIST += load-missing.il t-missing.cs load-exceptions.cs

load-missing.dll: load-missing.il
	$(Q) $(ILASM) /dll /output:$@ $<

load-exceptions.exe t.dll: load-exceptions.cs t-missing.cs load-missing.dll $(TEST_DRIVER_DEPEND)
	$(Q) $(MCS) -t:library -out:t.dll -d:FOUND t-missing.cs
	$(Q) $(MCS) -r:TestDriver.dll -r:load-missing.dll -r:t.dll -out:$@ $<
	$(Q) $(MCS) -t:library -out:t.dll t-missing.cs

test-type-load: load-exceptions.exe
if !POWERPC64
	$(Q) $(RUNTIME) load-exceptions.exe > load-exceptions.exe.stdout 2> load-exceptions.exe.stderr
endif

EXTRA_DIST += test-multi-netmodule-1-netmodule.cs test-multi-netmodule-2-dll1.cs test-multi-netmodule-3-dll2.cs test-multi-netmodule-4-exe.cs

test-multi-netmodule-1-netmodule.netmodule: test-multi-netmodule-1-netmodule.cs
	$(Q) $(MCS) -t:module -out:$@ $<
test-multi-netmodule-2-dll1.dll: test-multi-netmodule-2-dll1.cs test-multi-netmodule-1-netmodule.netmodule
	$(Q) $(MCS) -addmodule:test-multi-netmodule-1-netmodule.netmodule -t:library  -out:$@ $<
test-multi-netmodule-3-dll2.dll: test-multi-netmodule-3-dll2.cs test-multi-netmodule-1-netmodule.netmodule
	$(Q) $(MCS) -addmodule:test-multi-netmodule-1-netmodule.netmodule -t:library -out:$@ $<
test-multi-netmodule-4-exe.exe: test-multi-netmodule-4-exe.cs test-multi-netmodule-2-dll1.dll test-multi-netmodule-3-dll2.dll
	$(Q) $(MCS) -r:test-multi-netmodule-2-dll1.dll -out:$@ $<

test-multi-netmodule: test-multi-netmodule-4-exe.exe
	$(Q) $(RUNTIME) test-multi-netmodule-4-exe.exe > test-multi-netmodule-4-exe.exe.stdout 2> test-multi-netmodule-4-exe.exe.stderr

EXTRA_DIST += custom-attr-errors.cs custom-attr-errors-lib.cs

custom-attr-errors.exe custom-attr-errors-lib.dll: custom-attr-errors.cs custom-attr-errors-lib.cs $(TEST_DRIVER_DEPEND)
	$(Q) $(MCS) /t:library -D:WITH_MEMBERS custom-attr-errors-lib.cs
	$(Q) $(MCS) -r:TestDriver.dll -r:custom-attr-errors-lib.dll custom-attr-errors.cs
	$(Q) $(MCS) /t:library custom-attr-errors-lib.cs

test-cattr-type-load: custom-attr-errors.exe
	$(Q) $(RUNTIME) custom-attr-errors.exe > custom-attr-errors.exe.stdout 2> custom-attr-errors.exe.stderr

EXTRA_DIST += reflection-load-with-context-lib.cs reflection-load-with-context-second-lib.cs reflection-load-with-context.cs

reflection-load-with-context-second-lib.dll: reflection-load-with-context-second-lib.cs
	$(Q) $(MCS) /t:library -out:$@ $<
reflection-load-with-context-lib.dll: reflection-load-with-context-lib.cs reflection-load-with-context-second-lib.dll
	$(Q) $(MCS) /t:library -r:reflection-load-with-context-second-lib.dll -out:$@ $<
reflection-load-with-context.exe: reflection-load-with-context-lib.dll

test-reflection-load-with-context: reflection-load-with-context.exe
	$(Q) $(RUNTIME) reflection-load-with-context.exe > reflection-load-with-context.exe.stdout 2> reflection-load-with-context.exe.stderr


EXTRA_DIST += debug-casts.cs
# This depends on TLS, so its not ran by default
debug-casts: debug-casts.exe
	$(Q) $(RUNTIME) --debug=casts debug-casts.exe

EXTRA_DIST += sgen-bridge.cs sgen-descriptors.cs sgen-gshared-vtype.cs sgen-bridge-major-fragmentation.cs sgen-domain-unload.cs sgen-weakref-stress.cs sgen-cementing-stress.cs sgen-case-23400.cs 	finalizer-wait.cs critical-finalizers.cs sgen-domain-unload-2.cs sgen-suspend.cs sgen-new-threads-dont-join-stw.cs sgen-new-threads-dont-join-stw-2.cs sgen-new-threads-collect.cs sgen-bridge-xref.cs bug-17590.cs sgen-toggleref.cs sgen-bridge-gchandle.cs

test-sgen:
	ok=; \
	$(MAKE) test-sgen-regular || ok=false; \
	$(MAKE) test-sgen-toggleref || ok=false; \
	$(MAKE) test-sgen-bridge || ok=false; \
	$(MAKE) test-sgen-bridge2 || ok=false; \
	$(MAKE) test-sgen-bridge3 || ok=false; \
	$$ok

test-stress-sgen:
	ok=; \
	$(MAKE) test-stress-sgen-regular || ok=false; \
	$(MAKE) test-stress-sgen-toggleref || ok=false; \
	$(MAKE) test-stress-sgen-bridge || ok=false; \
	$(MAKE) test-stress-sgen-bridge2 || ok=false; \
	$(MAKE) test-stress-sgen-bridge3 || ok=false; \
	$$ok

TESTS_SGEN_REGULAR_SRC =	\
	finalizer-wait.cs	\
	critical-finalizers.cs	\
	sgen-descriptors.cs	\
	sgen-gshared-vtype.cs	\
	sgen-weakref-stress.cs	\
	sgen-cementing-stress.cs	\
	sgen-case-23400.cs	\
	sgen-new-threads-dont-join-stw.cs	\
	sgen-new-threads-dont-join-stw-2.cs	\
	sgen-new-threads-collect.cs	\
	gc-graystack-stress.cs	\
	bug-17590.cs	\
	sgen-domain-unload.cs	\
	sgen-domain-unload-2.cs

TESTS_SGEN_REGULAR=$(filter-out $(DISABLED_TESTS),$(TESTS_SGEN_REGULAR_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_SGEN_REGULAR=$(TESTS_SGEN_REGULAR:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_SGEN_REGULAR=$(TESTS_SGEN_REGULAR:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-sgen-regular: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR)
	ok=; \
	$(MAKE) test-sgen-regular-ms-simple || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-simple || ok=false; \
	$$ok

test-stress-sgen-regular: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR)
	ok=; \
	$(MAKE) test-sgen-regular-ms-simple MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-simple MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-split MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-split-95-clear-at-gc MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par-512k MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par-32m MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par-clear-at-gc MONO_TESTS_STRESS=1 || ok=false; \
	$$ok

test-sgen-regular-ms-simple: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep,minor=simple" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-simple: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc,minor=simple" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc-par,minor=simple" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-split: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc,minor=split" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-split-95-clear-at-gc: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=clear-at-gc --gc-params=major=marksweep-conc,minor=split,alloc-ratio=95" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple-par: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc-par,minor=simple-par" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple-par-512k: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=512k" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple-par-32m: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=32m" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple-par-clear-at-gc: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=clear-at-gc --gc-params=major=marksweep-conc-par,minor=simple-par" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)

TESTS_SGEN_TOGGLEREF_SRC=	\
	sgen-toggleref.cs

TESTS_SGEN_TOGGLEREF=$(filter-out $(DISABLED_TESTS),$(TESTS_SGEN_TOGGLEREF_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_SGEN_TOGGLEREF=$(TESTS_SGEN_TOGGLEREF:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_SGEN_TOGGLEREF=$(TESTS_SGEN_TOGGLEREF:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-sgen-toggleref: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF)
	ok=; \
	$(MAKE) test-sgen-toggleref-ms-simple || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-simple || ok=false; \
	$$ok

test-stress-sgen-toggleref: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF)
	ok=; \
	$(MAKE) test-sgen-toggleref-ms-simple MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-simple MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-split MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-split-95-clear-at-gc MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par-512k MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par-32m MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par-clear-at-gc MONO_TESTS_STRESS=1 || ok=false; \
	$$ok

test-sgen-toggleref-ms-simple: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep,minor=simple" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-simple: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc,minor=simple" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-split: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc,minor=split" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-split-95-clear-at-gc: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=clear-at-gc --gc-params=toggleref-test,major=marksweep-conc,minor=split,alloc-ratio=95" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple-par: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple-par-512k: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par,nursery-size=512k" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple-par-32m: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par,nursery-size=32m" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple-par-clear-at-gc: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=clear-at-gc --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)

TESTS_SGEN_BRIDGE_SRC=	\
	sgen-bridge.cs	\
	sgen-bridge-major-fragmentation.cs

TESTS_SGEN_BRIDGE=$(filter-out $(DISABLED_TESTS),$(TESTS_SGEN_BRIDGE_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_SGEN_BRIDGE=$(TESTS_SGEN_BRIDGE:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_SGEN_BRIDGE=$(TESTS_SGEN_BRIDGE:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-sgen-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE)
	ok=; \
	$(MAKE) test-sgen-bridge-ms-simple-tarjan-bridge || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-simple-tarjan-bridge || ok=false; \
	$$ok

test-stress-sgen-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE)
	ok=; \
	$(MAKE) test-sgen-bridge-ms-simple-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-simple-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-split-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-simple-new-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-simple-old-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-par-simple-par-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-par-simple-par-512k-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge-ms-conc-par-simple-par-32m-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$$ok

test-sgen-bridge-ms-simple-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep,minor=simple,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-simple-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-split-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc,minor=split,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-simple-new-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=new" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-simple-old-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=old" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-par-simple-par-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-par-simple-par-512k-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=512k,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-par-simple-par-32m-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=32m,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)

test-sgen-bridge-ms: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=Bridge --gc-params=" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=Bridge --gc-params=major=marksweep-conc" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-new-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=Bridge --gc-params=bridge-implementation=new" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-new-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=Bridge --gc-params=bridge-implementation=new,major=marksweep-conc" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-split-new-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=Bridge --gc-params=bridge-implementation=new,minor=split" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=Bridge --gc-params=bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-split-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=Bridge --gc-params=bridge-implementation=tarjan,minor=split" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)

TESTS_SGEN_BRIDGE2_SRC=	\
	sgen-bridge-xref.cs

TESTS_SGEN_BRIDGE2=$(filter-out $(DISABLED_TESTS),$(TESTS_SGEN_BRIDGE2_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_SGEN_BRIDGE2=$(TESTS_SGEN_BRIDGE2:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_SGEN_BRIDGE2=$(TESTS_SGEN_BRIDGE2:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-sgen-bridge2: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2)
	ok=; \
	$(MAKE) test-sgen-bridge2-ms-simple-tarjan-bridge || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-simple-tarjan-bridge || ok=false; \
	$$ok

test-stress-sgen-bridge2: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2)
	ok=; \
	$(MAKE) test-sgen-bridge2-ms-simple-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-simple-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-split-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-simple-new-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-simple-old-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-par-simple-par-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-par-simple-par-512k-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge2-ms-conc-par-simple-par-32m-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$$ok

test-sgen-bridge2-ms-simple-tarjan-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep,minor=simple,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
test-sgen-bridge2-ms-conc-simple-tarjan-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
test-sgen-bridge2-ms-conc-split-tarjan-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc,minor=split,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
test-sgen-bridge2-ms-conc-simple-new-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=new" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
test-sgen-bridge2-ms-conc-simple-old-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=old" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
test-sgen-bridge2-ms-conc-par-simple-par-tarjan-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
test-sgen-bridge2-ms-conc-par-simple-par-512k-tarjan-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=512k,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
test-sgen-bridge2-ms-conc-par-simple-par-32m-tarjan-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=32m,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)

TESTS_SGEN_BRIDGE3_SRC=	\
	sgen-bridge-gchandle.cs

TESTS_SGEN_BRIDGE3=$(filter-out $(DISABLED_TESTS),$(TESTS_SGEN_BRIDGE3_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_SGEN_BRIDGE3=$(TESTS_SGEN_BRIDGE3:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_SGEN_BRIDGE3=$(TESTS_SGEN_BRIDGE3:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-sgen-bridge3: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3)
	ok=; \
	$(MAKE) test-sgen-bridge3-ms-simple-tarjan-bridge || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-simple-tarjan-bridge || ok=false; \
	$$ok

test-stress-sgen-bridge3: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3)
	ok=; \
	$(MAKE) test-sgen-bridge3-ms-simple-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-simple-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-split-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-simple-new-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-simple-old-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-par-simple-par-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-par-simple-par-512k-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-bridge3-ms-conc-par-simple-par-32m-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
	$$ok

test-sgen-bridge3-ms-simple-tarjan-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep,minor=simple,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
test-sgen-bridge3-ms-conc-simple-tarjan-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
test-sgen-bridge3-ms-conc-split-tarjan-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc,minor=split,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
test-sgen-bridge3-ms-conc-simple-new-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=new" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
test-sgen-bridge3-ms-conc-simple-old-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc,minor=simple,bridge-implementation=old" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
test-sgen-bridge3-ms-conc-par-simple-par-tarjan-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
test-sgen-bridge3-ms-conc-par-simple-par-512k-tarjan-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=512k,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
test-sgen-bridge3-ms-conc-par-simple-par-32m-tarjan-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=32m,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)

TESTS_SGEN_OOM_SRC =	\
	sgen-oom-cementing-stress.cs	\
	sgen-oom-new-threads-dont-join-stw-2.cs	\
	sgen-oom-bridge-major-fragmentation.cs \
	gc-oom-handling.cs	\
	gc-oom-handling2.cs

TESTS_SGEN_OOM=$(filter-out $(DISABLED_TESTS),$(TESTS_SGEN_OOM_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_SGEN_OOM=$(TESTS_SGEN_OOM:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_SGEN_OOM=$(TESTS_SGEN_OOM:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

tests-sgen-oom: $(TESTS_SGEN_OOM) $(TESTSAOT_SGEN_OOM) test-runner.exe sgen-cementing-stress.exe sgen-new-threads-dont-join-stw-2.exe sgen-bridge-major-fragmentation.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) $(TEST_RUNNER_ARGS) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=max-valloc-size=50M,bridge=Bridge" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_OOM)

AOT_CONFIGURATIONS=	\
	"|regular"	\
	"--gc=boehm|boehm"

#LLVM crashes, got to fix it first.
#	"--llvm|llvm"	\
#	"--llvm --gc=boehm|llvm+boehm"


test-aot:
	@failed=0; \
	passed=0; \
	failed_tests="";\
	profile=$(DEFAULT_PROFILE);	\
	tmpfile=`mktemp -t mono_aot_outputXXXXXX` || exit 1;	\
	rm -f test-aot-$${name}.stdout test-aot-$${name}.stderr; \
	for assembly in $(mcs_topdir)/class/lib/$$profile/*.dll	; do	\
		asm_name=`basename $$assembly`;	\
		echo "... $$asm_name";	\
		for conf in $(AOT_CONFIGURATIONS); do	\
			name=`echo $$conf | cut -d\| -f 2`;	\
			params=`echo $$conf | cut -d\| -f 1`;	\
			test_name="$${asm_name}|$${name}";	\
			echo "  $$test_name";	\
			if MONO_PATH=$(mcs_topdir)/class/lib/$$profile $(JITTEST_PROG_RUN) $$params --aot=outfile=$$tmpfile $$assembly >> test-aot-$${name}.stdout 2>> test-aot-$${name}.stderr;	\
			then \
				passed=`expr $${passed} + 1`; \
			else \
				failed=`expr $${failed} + 1`; \
				failed_tests="$${failed_tests} $$test_name"; \
			fi \
		done;	\
	done;	\
	rm	$$tmpfile;	\
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $${failed} != 0 ]; then echo -e "\nFailed tests:\n"; \
	for i in $${failed_tests}; do echo $${i}; done; exit 1; fi


# Generated tests for runtime invoke
EXTRA_DIST += gen-runtime-invoke.cs

runtime-invoke.gen.cs: gen-runtime-invoke.exe
	$(Q) $(RUNTIME) gen-runtime-invoke.exe > runtime-invoke.gen.cs

EXTRA_DIST += make-imt-test.cs

imt_big_iface_test.cs: make-imt-test.exe
	$(Q) $(RUNTIME) make-imt-test.exe > imt_big_iface_test.cs

EXTRA_DIST += test-inline-call-stack-library.cs test-inline-call-stack.cs

test-inline-call-stack-library.dll: $(srcdir)/test-inline-call-stack-library.cs
	$(MCS) -t:library -out:$@ $<

test-inline-call-stack.exe$(PLATFORM_AOT_SUFFIX): test-inline-call-stack-library.dll$(PLATFORM_AOT_SUFFIX)
test-inline-call-stack.exe: test-inline-call-stack.cs test-inline-call-stack-library.dll $(TEST_DRIVER_DEPEND)
	$(MCS) -r:TestDriver.dll -r:test-inline-call-stack-library.dll -out:$@ $<

EXTRA_DIST += unhandled-exception-base-configuration.config
EXTRA_DIST += unhandled-exception-legacy-configuration.config
EXTRA_DIST += appdomain-unload.exe.config
EXTRA_DIST += unhandled-exception-test-case.2.cs
EXTRA_DIST += unhandled-exception-test-runner.2.cs
unhandled-exception-test-case.1.cs: unhandled-exception-test-case.2.cs
	cp unhandled-exception-test-case.2.cs unhandled-exception-test-case.1.cs
unhandled-exception-test-case-legacy.1.cs: unhandled-exception-test-case.1.cs
	cp unhandled-exception-test-case.1.cs unhandled-exception-test-case-legacy.1.cs
unhandled-exception-test-case-legacy.2.cs: unhandled-exception-test-case.2.cs
	cp unhandled-exception-test-case.2.cs unhandled-exception-test-case-legacy.2.cs
unhandled-exception-config_files: unhandled-exception-base-configuration.config unhandled-exception-legacy-configuration.config
	cp unhandled-exception-base-configuration.config unhandled-exception-test-case.1.exe.config
	cp unhandled-exception-base-configuration.config unhandled-exception-test-case.2.exe.config
	cp unhandled-exception-legacy-configuration.config unhandled-exception-test-case-legacy.1.exe.config
	cp unhandled-exception-legacy-configuration.config unhandled-exception-test-case-legacy.2.exe.config

unhandled-exception-test-runner.2.exe: unhandled-exception-config_files unhandled-exception-test-case.1.exe unhandled-exception-test-case-legacy.1.exe  unhandled-exception-test-case.2.exe unhandled-exception-test-case-legacy.2.exe

test-unhandled-exception: unhandled-exception-test-runner.2.exe
	$(Q) $(RUNTIME) unhandled-exception-test-runner.2.exe RUNTIME:../mini/mono,GTC:F

safehandle.2.exe winx64structs.exe thunks.exe pinvoke3.exe pinvoke2.exe pinvoke-2.2.exe pinvoke17.exe pinvoke13.exe \
	pinvoke11.exe pinvoke_ppcs.exe pinvoke_ppci.exe pinvoke_ppcf.exe pinvoke_ppcd.exe pinvoke_ppcc.exe pinvoke.exe \
	marshalbool.exe marshal9.exe marshal5.exe marshal.exe handleref.exe cominterop.exe bug-Xamarin-5278.exe: libtest.la

event-get.2.exe$(PLATFORM_AOT_SUFFIX): event-il.exe$(PLATFORM_AOT_SUFFIX)
event-get.2.exe: event-il.exe

module-cctor-loader.2.exe$(PLATFORM_AOT_SUFFIX): module-cctor.exe$(PLATFORM_AOT_SUFFIX)
module-cctor-loader.2.exe: module-cctor.exe

reference-loader.exe$(PLATFORM_AOT_SUFFIX): TestingReferenceAssembly.dll$(PLATFORM_AOT_SUFFIX) TestingReferenceReferenceAssembly.dll$(PLATFORM_AOT_SUFFIX)
reference-loader.exe: TestingReferenceAssembly.dll TestingReferenceReferenceAssembly.dll

assemblyresolve_asm.dll$(PLATFORM_AOT_SUFFIX): assemblyresolve_deps/Test.dll$(PLATFORM_AOT_SUFFIX)
	MONO_PATH="assemblyresolve_deps:$(CLASS)" $(top_builddir)/runtime/mono-wrapper $(AOT_BUILD_FLAGS) assemblyresolve_asm.dll
assemblyresolve_deps/Test.dll$(PLATFORM_AOT_SUFFIX): assemblyresolve_deps/TestBase.dll$(PLATFORM_AOT_SUFFIX)

assemblyresolve_deps:
	mkdir -p assemblyresolve_deps
assemblyresolve_deps/TestBase.dll: assemblyresolve_deps $(srcdir)/assemblyresolve_TestBase.cs
	$(MCS) -target:library -out:assemblyresolve_deps/TestBase.dll $(srcdir)/assemblyresolve_TestBase.cs
assemblyresolve_deps/Test.dll: assemblyresolve_deps assemblyresolve_deps/TestBase.dll $(srcdir)/assemblyresolve_Test.cs
	$(MCS) -target:library -r:assemblyresolve_deps/TestBase.dll -out:assemblyresolve_deps/Test.dll $(srcdir)/assemblyresolve_Test.cs
assemblyresolve_asm.dll: assemblyresolve_deps/Test.dll $(srcdir)/assemblyresolve_asm.cs
	$(MCS) -target:library -r:assemblyresolve_deps/TestBase.dll -r:assemblyresolve_deps/Test.dll -out:assemblyresolve_asm.dll $(srcdir)/assemblyresolve_asm.cs

assemblyresolve_event3.exe$(PLATFORM_AOT_SUFFIX): assemblyresolve_asm.dll$(PLATFORM_AOT_SUFFIX) assemblyresolve_deps/Test.dll$(PLATFORM_AOT_SUFFIX) assemblyresolve_deps/TestBase.dll$(PLATFORM_AOT_SUFFIX)
assemblyresolve_event3.exe: assemblyresolve_asm.dll assemblyresolve_deps/Test.dll assemblyresolve_deps/TestBase.dll

assemblyresolve_event4.exe$(PLATFORM_AOT_SUFFIX): assemblyresolve_deps/Test.dll$(PLATFORM_AOT_SUFFIX) assemblyresolve_deps/TestBase.dll$(PLATFORM_AOT_SUFFIX)
assemblyresolve_event4.exe: assemblyresolve_deps/Test.dll assemblyresolve_deps/TestBase.dll

# We use 'test-support-files' to handle an ordering issue between the 'mono/' and 'runtime/' directories
bug-80307.exe: $(srcdir)/bug-80307.cs
	$(MCS) -r:$(CLASS)/System.Web.dll -out:$@ $(srcdir)/bug-80307.cs
	cd $(top_builddir)/runtime && $(MAKE) test-support-files

EXTRA_DIST += bug-81673-interface.cs

bug-81673.exe$(PLATFORM_AOT_SUFFIX): bug-81673-interface.dll$(PLATFORM_AOT_SUFFIX)
bug-81673.exe bug-81673-interface.dll: $(srcdir)/bug-81673.cs $(srcdir)/bug-81673-interface.cs
	$(MCS) -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs
	$(MCS) -r:bug-81673-interface.dll -out:bug-81673.exe $(srcdir)/bug-81673.cs
	$(MCS) -define:WITH_STOP -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs

EXTRA_DIST += bug-36848-a.cs

bug-36848.exe$(PLATFORM_AOT_SUFFIX): bug-36848-a.dll$(PLATFORM_AOT_SUFFIX)
bug-36848.exe bug-36848-a.dll: $(srcdir)/bug-36848.cs $(srcdir)/bug-36848-a.cs
	$(MCS) -target:library -out:bug-36848-a.dll $(srcdir)/bug-36848-a.cs
	$(MCS) -r:bug-36848-a.dll -out:bug-36848.exe $(srcdir)/bug-36848.cs
	$(MCS) -define:WITH_STOP -target:library -out:bug-36848-a.dll $(srcdir)/bug-36848-a.cs

EXTRA_DIST += bug-81691-a.cs bug-81691-b.cs

bug-81691.exe$(PLATFORM_AOT_SUFFIX): bug-81691-b.dll$(PLATFORM_AOT_SUFFIX)
bug-81691.exe bug-81691-a.dll bug-81691-b.dll: $(srcdir)/bug-81691.cs $(srcdir)/bug-81691-a.cs $(srcdir)/bug-81691-b.cs
	$(MCS) -target:library -out:bug-81691-a.dll $(srcdir)/bug-81691-a.cs 
	$(MCS) -r:bug-81691-a.dll -target:library -out:bug-81691-b.dll $(srcdir)/bug-81691-b.cs
	$(MCS) -r:bug-81691-b.dll -out:bug-81691.exe $(srcdir)/bug-81691.cs
	rm -f bug-81691-a.dll

EXTRA_DIST += bug-81466-lib.il

bug-81466.exe$(PLATFORM_AOT_SUFFIX): bug-81466-lib.dll$(PLATFORM_AOT_SUFFIX)
bug-81466.exe bug-81466-lib.dll: $(srcdir)/bug-81466.il $(srcdir)/bug-81466-lib.il
	$(ILASM) /dll /output:bug-81466-lib.dll $(srcdir)/bug-81466-lib.il
	$(ILASM) /exe /output:bug-81466.exe $(srcdir)/bug-81466.il

EXTRA_DIST += bug-324535-il.il

bug-324535.exe$(PLATFORM_AOT_SUFFIX): bug-324535-il.dll$(PLATFORM_AOT_SUFFIX)
bug-324535.exe bug-324535-il.dll: $(srcdir)/bug-324535.cs $(srcdir)/bug-324535-il.il
	$(ILASM) /dll /output:bug-324535-il.dll $(srcdir)/bug-324535-il.il
	$(MCS) -r:bug-324535-il.dll -out:bug-324535.exe $(srcdir)/bug-324535.cs

EXTRA_DIST += custom-modifiers.2.cs custom-modifiers-lib.il

custom-modifiers.2.exe$(PLATFORM_AOT_SUFFIX): custom-modifiers-lib.dll$(PLATFORM_AOT_SUFFIX)
custom-modifiers.2.exe custom-modifiers-lib.dll: $(srcdir)/custom-modifiers.2.cs $(srcdir)/custom-modifiers-lib.il
	$(ILASM) /dll /output:custom-modifiers-lib.dll $(srcdir)/custom-modifiers-lib.il
	$(MCS) -r:custom-modifiers-lib.dll -out:custom-modifiers.2.exe $(srcdir)/custom-modifiers.2.cs

EXTRA_DIST += bug-382986-lib.cs

bug-382986.exe$(PLATFORM_AOT_SUFFIX): bug-382986-lib.dll$(PLATFORM_AOT_SUFFIX)
bug-382986.exe bug-382986-lib.dll: $(srcdir)/bug-382986.cs $(srcdir)/bug-382986-lib.cs
	$(MCS) -target:library -out:bug-382986-lib.dll $(srcdir)/bug-382986-lib.cs
	$(MCS) -r:bug-382986-lib.dll -out:bug-382986.exe $(srcdir)/bug-382986.cs

EXTRA_DIST += bug-17537-helper.cs

bug-17537.exe$(PLATFORM_AOT_SUFFIX): bug-17537-helper.exe$(PLATFORM_AOT_SUFFIX)
bug-17537-helper.exe: $(srcdir)/bug-17537-helper.cs
	$(MCS) -out:$@ $<
	chmod -x $@
bug-17537.exe: bug-17537-helper.exe

EXTRA_DIST += coreclr-security.cs

test-coreclr-security : coreclr-security.exe
	@$(RUNTIME_MOONLIGHT) --security=core-clr-test coreclr-security.exe

EXTRA_DIST += generic-unboxing.2.il generic-boxing.2.il

generic-unboxing.2.dll: $(srcdir)/generic-unboxing.2.il
	$(ILASM) /dll /output:$@ $<
generic-boxing.2.dll: $(srcdir)/generic-boxing.2.il generic-unboxing.2.dll
	$(ILASM) /dll /output:$@ $<

EXTRA_DIST += generic-unbox.2.cs

generic-unbox.2.exe$(PLATFORM_AOT_SUFFIX): generic-unboxing.2.dll$(PLATFORM_AOT_SUFFIX)
generic-unbox.2.exe: $(srcdir)/generic-unbox.2.cs generic-unboxing.2.dll
	$(MCS) -r:generic-unboxing.2.dll -out:$@ $<

EXTRA_DIST += generic-box.2.cs

generic-box.2.exe$(PLATFORM_AOT_SUFFIX): generic-unboxing.2.dll$(PLATFORM_AOT_SUFFIX) generic-boxing.2.dll$(PLATFORM_AOT_SUFFIX)
generic-box.2.exe : $(srcdir)/generic-box.2.cs generic-unboxing.2.dll generic-boxing.2.dll
	$(MCS) -r:generic-unboxing.2.dll,generic-boxing.2.dll -out:$@ $<

EXTRA_DIST += generic-delegate2.2.cs generic-delegate2-lib.2.il

generic-delegate2-lib.2.dll: $(srcdir)/generic-delegate2-lib.2.il
	$(ILASM) /dll /output:$@ $<

generic-delegate2.2.exe$(PLATFORM_AOT_SUFFIX): generic-delegate2-lib.2.dll$(PLATFORM_AOT_SUFFIX)
generic-delegate2.2.exe: $(srcdir)/generic-delegate2.2.cs generic-delegate2-lib.2.dll
	$(MCS) -r:generic-delegate2-lib.2.dll -out:$@ $<

bug-3903.exe: bug-3903.cs
	$(MCS_NO_LIB)  $(srcdir)/bug-3903.cs -nostdlib -r:$(srcdir)/../../external/binary-reference-assemblies/v2.0/mscorlib.dll -r:$(srcdir)/../../external/binary-reference-assemblies/v2.0/System.Core.dll -out:$@

gshared:
	$(MAKE) test-generic-sharing

gshared-aot:
	@$(MAKE) AOT=1 gshared

test-generic-sharing-normal: $(TESTS_GSHARED) $(TESTSAOT_GSHARED)
	@for fn in $+ ; do	\
		echo "Testing $$fn ...";	\
		$(RUNTIME) -O=gshared                $$fn > $$fn.stdout || exit 1;	\
		$(RUNTIME) -O=gshared,shared         $$fn > $$fn.stdout || exit 1;	\
		$(RUNTIME) -O=gshared,-inline        $$fn > $$fn.stdout || exit 1;	\
		$(RUNTIME) -O=gshared,-inline,shared $$fn > $fn.stdout || exit 1;	\
		if [ x$(AOT) = x1 ]; then $(with_mono_path) $(JITTEST_PROG_RUN) --aot --debug $$fn > /dev/null || exit 1; $(RUNTIME) $$fn > $$fn.stdout || exit 1; fi; \
	done

test-generic-sharing-managed: test-runner.exe $(TESTS_GSHARED) $(TESTSAOT_GSHARED) 
	$(Q) $(TOOLS_RUNTIME) $(TEST_RUNNER) -j a --testsuite-name "gshared" --disabled "$(DISABLED_TESTS)" --opt-sets "gshared gshared,shared gshared,-inline gshared,-inline,shared" $(TESTS_GSHARED)

if NACL_CODEGEN
test-generic-sharing:
else
test-generic-sharing:
	@if test x$(M) != x0; then $(MAKE) test-generic-sharing-managed; else $(MAKE) test-generic-sharing-normal; fi
endif

EXTRA_DIST += async-exceptions.cs
async-exceptions.exe : async-exceptions.cs
	$(MCS) -out:async-exceptions.exe $(srcdir)/async-exceptions.cs
test-async-exceptions : async-exceptions.exe
	for i in `echo 0 1 2 3 4 5 6 7 8 9 10`; do $(RUNTIME) --inject-async-exc Tests:foo $$i async-exceptions.exe || exit 1; done
	for i in `echo 0 1 2 3 4 5 6 7 8 9 10`; do $(RUNTIME) --inject-async-exc Tests:bar $$i async-exceptions.exe || exit 1; done

EXTRA_DIST += modules.cs modules-m1.cs
modules-m1.netmodule: modules-m1.cs
	$(MCS) -out:$@ /target:module $(srcdir)/modules-m1.cs
modules.exe: modules.cs modules-m1.netmodule $(TEST_DRIVER_DEPEND) 
	$(MCS) -out:$@ /addmodule:modules-m1.netmodule -r:TestDriver.dll $(srcdir)/modules.cs

# Useful if mono is compiled with --enable-shared=no
patch-libtool:
	cp "../../libtool" .
	sed -e 's,build_libtool_libs=no,build_libtool_libs=yes,g' libtool > 2; mv 2 libtool
	sed -e 's,LIBTOOL =,LIBTOOL2 =,g' Makefile > 2 && echo "LIBTOOL = bash ./libtool" > 1 && cat 1 2 > Makefile
	touch libtest.c


if NACL_CODEGEN
test-process-exit:
else
EXTRA_DIST += threadpool-in-processexit.cs threadpool-in-processexit.exe.stdout.expected
test-process-exit:
	@$(MCS) $(srcdir)/threadpool-in-processexit.cs -out:threadpool-in-processexit.exe
	@echo "Testing threadpool-in-processexit.exe..."
	@$(RUNTIME) threadpool-in-processexit.exe > threadpool-in-processexit.exe.stdout
	@diff -w threadpool-in-processexit.exe.stdout $(srcdir)/threadpool-in-processexit.exe.stdout.expected
endif

# tests that expect a 1 exit code
TESTS_UNHANDLED_EXCEPTION_1_SRC =	\
	unhandled-exception-1.cs

# tests that expect a 255 exit code
TESTS_UNHANDLED_EXCEPTION_255_SRC =	\
	unhandled-exception-2.cs	\
	unhandled-exception-3.cs	\
	unhandled-exception-4.cs	\
	unhandled-exception-5.cs	\
	unhandled-exception-6.cs	\
	unhandled-exception-7.cs	\
	unhandled-exception-8.cs

TESTS_UNHANDLED_EXCEPTION_1=$(filter-out $(DISABLED_TESTS),$(TESTS_UNHANDLED_EXCEPTION_1_SRC:.cs=.exe))
TESTS_UNHANDLED_EXCEPTION_255=$(filter-out $(DISABLED_TESTS),$(TESTS_UNHANDLED_EXCEPTION_255_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_UNHANDLED_EXCEPTION_1=$(TESTS_UNHANDLED_EXCEPTION_1:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_UNHANDLED_EXCEPTION_255=$(TESTS_UNHANDLED_EXCEPTION_255:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_UNHANDLED_EXCEPTION_1=$(TESTS_UNHANDLED_EXCEPTION_1:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_UNHANDLED_EXCEPTION_255=$(TESTS_UNHANDLED_EXCEPTION_255:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-unhandled-exception-2: $(TESTS_UNHANDLED_EXCEPTION_1) $(TESTS_UNHANDLED_EXCEPTION_255) $(TESTSAOT_UNHANDLED_EXCEPTION_1) $(TESTSAOT_UNHANDLED_EXCEPTION_255)
	ok=; \
	$(MAKE) test-unhandled-exception-2-1-with-managed-handler || ok=false; \
	$(MAKE) test-unhandled-exception-2-1-without-managed-handler || ok=false; \
	$(MAKE) test-unhandled-exception-2-255-with-managed-handler || ok=false; \
	$(MAKE) test-unhandled-exception-2-255-without-managed-handler || ok=false; \
	$$ok

test-unhandled-exception-2-1-with-managed-handler: $(TESTS_UNHANDLED_EXCEPTION_1) $(TESTSAOT_UNHANDLED_EXCEPTION_1) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) -j a --testsuite-name $@ --disabled "$(DISABLED_TESTS)" --expected-exit-code 1 $(TESTS_UNHANDLED_EXCEPTION_1)
test-unhandled-exception-2-1-without-managed-handler: $(TESTS_UNHANDLED_EXCEPTION_1) $(TESTSAOT_UNHANDLED_EXCEPTION_1) test-runner.exe
	TEST_UNHANDLED_EXCEPTION_HANDLER=1 $(TOOLS_RUNTIME) $(TEST_RUNNER) -j a --testsuite-name $@ --disabled "$(DISABLED_TESTS)" --expected-exit-code 1 $(TESTS_UNHANDLED_EXCEPTION_1)
test-unhandled-exception-2-255-with-managed-handler: $(TESTS_UNHANDLED_EXCEPTION_255) $(TESTSAOT_UNHANDLED_EXCEPTION_255) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) -j a --testsuite-name $@ --disabled "$(DISABLED_TESTS)" --expected-exit-code 255 $(TESTS_UNHANDLED_EXCEPTION_255)
test-unhandled-exception-2-255-without-managed-handler: $(TESTS_UNHANDLED_EXCEPTION_255) $(TESTSAOT_UNHANDLED_EXCEPTION_255) test-runner.exe
	TEST_UNHANDLED_EXCEPTION_HANDLER=1 $(TOOLS_RUNTIME) $(TEST_RUNNER) -j a --testsuite-name $@ --disabled "$(DISABLED_TESTS)" --expected-exit-code 255 $(TESTS_UNHANDLED_EXCEPTION_255)

EXTRA_DIST += appdomain-loader.cs appdomain-tester.cs

if FULL_AOT_TESTS
test-appdomain-unload:
else
appdomain-loader.exe: appdomain-tester.exe

test-appdomain-unload: appdomain-loader.exe appdomain-tester.exe
	$(RUNTIME) -O=gshared appdomain-loader.exe > appdomain-loader.exe.1.stdout || exit 1;
	$(RUNTIME) appdomain-loader.exe > appdomain-loader.exe.2.stdout || exit 1;
	MONO_DEBUG_ASSEMBLY_UNLOAD=1 $(RUNTIME) -O=gshared appdomain-loader.exe > appdomain-loader.exe.3.stdout || exit 1;
	MONO_DEBUG_ASSEMBLY_UNLOAD=1 $(RUNTIME) appdomain-loader.exe > appdomain-loader.exe.4.stdout || exit 1;
endif

EXTRA_DIST += console-output.cs console-output.exe.stderr.expected console-output.exe.stdout.expected
test-console-output: console-output.exe
	@$(RUNTIME) console-output.exe 1>console-output.exe.stdout
	@$(RUNTIME) console-output.exe 2>console-output.exe.stderr
	@diff -w console-output.exe.stdout $(srcdir)/console-output.exe.stdout.expected \
		&& diff -w console-output.exe.stderr $(srcdir)/console-output.exe.stderr.expected

test-pedump: test-runner.exe
	$(with_mono_path) $(mono_build_root)/tools/pedump/pedump --verify error test-runner.exe

.PHONY: test-gac-loading

runtest-gac-loading: test-runner.exe
	$(MAKE) -C testing_gac runtest

compile-gac-loading:
	$(MAKE) -C testing_gac compile-tests

TESTS_STRESS_PROCESS_SRC=	\
		process-stress-1.cs	\
		process-stress-2.cs	\
		process-stress-3.cs	\
		process-leak.cs

TESTS_STRESS_PROCESS=$(filter-out $(DISABLED_TESTS),$(TESTS_STRESS_PROCESS_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_STRESS_PROCESS=$(TESTS_STRESS_PROCESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_STRESS_PROCESS=$(TESTS_STRESS_PROCESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-process-stress: $(TESTS_STRESS_PROCESS) $(TESTSAOT_STRESS_PROCESS) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --disabled "$(DISABLED_TESTS)" --timeout 600 $(TESTS_STRESS_PROCESS)

coreclr-gcstress:
	$(MAKE) -C $(mono_build_root)/acceptance-tests coreclr-gcstress

# Tests for the Mono lldb plugin
EXTRA_DIST += test_lldb.py test-lldb.cs
test-lldb: test-lldb.exe
	python test_lldb.py $(JITTEST_PROG)

noinst_LTLIBRARIES = libtest.la

AM_CPPFLAGS = $(GLIB_CFLAGS)

if HOST_WIN32
# gcc-3.4.4 emits incorrect code when making indirect calls to stdcall functions using a tail call
# This shows up when compiling mono_test_marshal_delegate ()
libtest_la_CFLAGS=-fno-optimize-sibling-calls
# the exported names created by gcc for stdcall functions are missing the leading _, so MS.NET
# can't find them. So we use --kill-at to remove the @ suffix as well.
libtest_la_LDFLAGS=-no-undefined -rpath `pwd` -Wl,--kill-at
else
libtest_la_LDFLAGS = -rpath `pwd`
endif
libtest_la_SOURCES = libtest.c
libtest_la_LIBADD = $(GLIB_LIBS) $(LIBICONV)

CLEANFILES = $(TESTS_CS) $(TESTS_IL) $(TESTS_BENCH) $(TESTS_STRESS) $(TESTSAOT_CS) $(TESTSAOT_IL) $(TESTSAOT_BENCH) $(TESTSAOT_STRESS) *.dll *.stdout *.aotlog *.exe stest.dat
