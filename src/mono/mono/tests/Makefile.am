MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

SUBDIRS = gc-descriptors . testing_gac assembly-load-reference llvmonly-mixed fullaot-mixed

check-local:
	ok=:; \
	$(MAKE) test-tailcall || ok=false; \
	if test x$(IGNORE_TEST_JIT) == x; then $(MAKE) test-jit || ok=false; fi; \
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
	$(MAKE) test-internalsvisibleto || ok=false; \
	$(MAKE) rm-empty-logs || ok=false; \
	$(MAKE) runtest-gac-loading || ok=false; \
	$(MAKE) runtest-assembly-load-reference || ok=false; \
	$$ok

BUILT_SOURCES =

ARRAY_COOP_CS=\
	array-coop-bigvt.cs     \
	array-coop-int.cs       \
	array-coop-smallvt.cs

BUILT_SOURCES += $(ARRAY_COOP_CS)

array-coop-bigvt.cs: array-coop-bigvt.sh array-coop-1.cs array-coop-2.cs
	 $(srcdir)/array-coop-bigvt.sh $(srcdir) > $@

array-coop-smallvt.cs: array-coop-smallvt.sh array-coop-1.cs array-coop-2.cs
	 $(srcdir)/array-coop-smallvt.sh $(srcdir) > $@

array-coop-int.cs: array-coop-int.sh array-coop-1.cs array-coop-2.cs
	 $(srcdir)/array-coop-int.sh $(srcdir) > $@

# These are not actually tests, but they are source files part of another test.
TAILCALL_DISABLED_TESTS_COMPILE = \
	tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyTransparentLibraryWithPrefix.exe \
	tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyVerifiableLibraryWithPrefix.exe

# FIXME valuetype parameter.
TAILCALL_DISABLED_TESTS_COMPILE += \
	tailcall/coreclr/JIT/opt/ETW/TailCallCases.exe

# FIXME Some of these sound like negative tests -- "badtailcall". Verify and automate.
# FIXME investigate and explain every test failure.
TAILCALL_DISABLED_TESTS_RUN = \
	tailcall/coreclr/JIT/Directed/IL/PInvokeTail/tailwinapi.exe \
	tailcall/coreclr/JIT/Directed/IL/Tailcall/Jittailcall2.exe \
	tailcall/coreclr/JIT/Directed/IL/mutualrecur-tailcall/MutualRecur-TailCall.exe \
	tailcall/coreclr/JIT/Directed/coverage/importer/Desktop/badtailcall.exe \
	tailcall/coreclr/JIT/Directed/coverage/importer/badtailcall.exe \
	tailcall/coreclr/JIT/Directed/pinvoke/tail.exe \
	tailcall/coreclr/JIT/IL_Conformance/Old/Base/tailcall.exe \
	tailcall/coreclr/JIT/Methodical/Invoke/fptr/recurse_tail_call.exe \
	tailcall/coreclr/JIT/Methodical/Invoke/fptr/recurse_tail_calli.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_array.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_array_nz.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/gcval_nested.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/pointer_i.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/pointer.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/reference_i.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/test_implicit.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/test_mutual_rec.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/test_switch.exe \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/delegateTail.exe \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/hijacking.exe \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/smallFrame.exe \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/tailcall_AV.exe \
	tailcall/coreclr/JIT/Regression/VS-ia64-JIT/V1.2-M02/b102844/tailcallcalli.exe \
	tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyWithPrefix.exe \
	$(TAILCALL_DISABLED_TESTS_COMPILE)

# FIXME investigate and explain every test failure.
TAILCALL_DISABLED_TESTS_RUN += \
	tailcall/coreclr/JIT/Methodical/Boxing/misc/tailjump.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_gc.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_inst.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_value.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_virt.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/gcval.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/gcval_sideeffect.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/test_3b.exe \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/delegateParamCallTarget.exe

if X86
TAILCALL_DISABLED_TESTS_RUN += tailcall/coreclr/JIT/Methodical/nonvirtualcall/tailcall.exe
endif

if FULL_AOT_TESTS

# FullAOT Assertion at method-to-ir.c:9203, condition `*ip == CEE_RET' not met
# This should raise a bad image format exception.
TAILCALL_DISABLED_TESTS_COMPILE += \
	tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyWithPrefix.exe

# return type mismatch (float32 vs. float64)
# 	tailcall/coreclr/JIT/Methodical/tailcall/compat_r8_r4_inl.exe
# 	tailcall/coreclr/JIT/Methodical/tailcall/compat_r4_r8_inl.exe
#
# return type mismatch (int32 vs. enum)
# 	tailcall/coreclr/JIT/Methodical/tailcall/compat_enum.exe
#
# FIXME depends on msvcrt.dll, and calli
#	tailcall/coreclr/JIT/Directed/IL/PInvokeTail/PInvokeTail.exe
#
# FIXME reference parameter; requires range check to allow
#	tailcall/coreclr/JIT/Directed/zeroinit/tail.exe

# Tests that fail assert about tailcall.
TAILCALL_DISABLED_TESTS_RUN += \
	tailcall/coreclr/JIT/Directed/IL/PInvokeTail/tailwinapi.exe \
	tailcall/coreclr/JIT/Directed/pinvoke/tail.exe \
	tailcall/coreclr/JIT/Methodical/Invoke/fptr/recurse_tail_call.exe \
	tailcall/coreclr/JIT/Methodical/Invoke/fptr/recurse_tail_calli.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/Desktop/thread-race.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_array_nz.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/pointer.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/reference_i.exe \
	tailcall/coreclr/JIT/Methodical/tailcall/test_mutual_rec.exe \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/delegateTail.exe \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/tailcall_AV.exe \
	tailcall/coreclr/JIT/Regression/VS-ia64-JIT/V1.2-M02/b102844/tailcallcalli.exe

# This test is also slow, though its sleep can be dialed down.
#TAILCALL_DISABLED_TESTS_RUN += tailcall/coreclr/JIT/Methodical/tailcall_v4/hijacking.exe

endif

compile-tailcall: $(TESTS_TAILCALL_COMPILE) $(TESTSAOT_TAILCALL)

if FULL_AOT_TESTS

# FullAOT does not allow --compile-all.

else

TEST_TAILCALL_COMPILE_ALL=--runtime-args "--compile-all"

endif

test-tailcall: compile-tailcall test-runner.exe
	MONO_DEBUG=test-tailcall-require $(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j a $(TEST_TAILCALL_COMPILE_ALL) --testsuite-name "runtime-tailcall" --timeout 300 --disabled "$(DISABLED_TESTS)" $(TESTS_TAILCALL_RUN)

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
	ok=; \
	$(MAKE) test-stress-sgen || ok=false; \
	$(MAKE) stresstest || ok=false; \
	$$ok

# for backwards compatibility on Wrench
test-wrench: check-parallel

aotcheck: testaot gshared-aot

if HOST_WIN32
JITTEST_PROG = $(top_builddir)/runtime/mono-wrapper
else
JITTEST_PROG = $(if $(VALGRIND), valgrind $(VALGRIND_ARGS),) $(if $(SGEN),$(top_builddir)/mono/mini/mono-sgen,$(top_builddir)/mono/mini/mono)
endif

JITTEST_PROG_RUN = MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(JITTEST_PROG)

RUNTIME_ARGS=--config tests-config --optimize=all --debug
TEST_RUNTIME_ARGS ?= $(RUNTIME_ARGS)
TEST_AOT_BUILD_FLAGS ?= $(AOT_BUILD_FLAGS)
TEST_AOT_RUN_FLAGS ?= $(AOT_RUN_FLAGS)

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

if FULL_AOT_INTERP_TESTS
PROFILE_MCS_FLAGS = -d:MOBILE,MOBILE_LEGACY
endif

MCS_NO_UNSAFE = $(TOOLS_RUNTIME) $(CSC) -debug:portable \
	-noconfig -nologo \
	-nowarn:0162 -nowarn:0168 -nowarn:0219 -nowarn:0414 -nowarn:0618 \
	-nowarn:0169 -nowarn:1690 -nowarn:0649 -nowarn:0612 -nowarn:3021 \
	-nowarn:0197 -langversion:latest $(PROFILE_MCS_FLAGS)
MCS_NO_LIB = $(MCS_NO_UNSAFE) -unsafe

MCS = $(MCS_NO_LIB)

ILASM = $(TOOLS_RUNTIME) $(mcs_topdir)/class/lib/build/ilasm.exe

TEST_RUNNER = ./test-runner.exe

if HOST_WIN32
if ENABLE_MSVC
DEFAULT_WIN32_RUNTIME=$(shell cygpath -w -a $(top_builddir)/@mono_runtime@ | sed 's/\\/\\\\/g')
else
DEFAULT_WIN32_RUNTIME=$(top_builddir)/runtime/mono-wrapper)
endif
TEST_RUNNER += --config tests-config --runtime "$(if $(MONO_EXECUTABLE),$(shell cygpath -w -a $(MONO_EXECUTABLE) | sed 's/\\/\\\\/g'),$(DEFAULT_WIN32_RUNTIME))"
else
TEST_RUNNER += --config tests-config --runtime "$(if $(MONO_EXECUTABLE),$(MONO_EXECUTABLE),$(top_builddir)/runtime/mono-wrapper)"
endif

TEST_RUNNER += --mono-path "$(CLASS)"

if FULL_AOT_TESTS
TEST_RUNNER += --runtime-args "$(TEST_AOT_RUN_FLAGS)"
endif

if HYBRID_AOT_TESTS
TEST_RUNNER += --runtime-args "$(TEST_AOT_RUN_FLAGS)"
endif

TEST_RUNNER += --runtime-args "$(TEST_RUNTIME_FLAGS)"

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
	spinlock-stress.cs 	\
	assembly-load-stress.cs

# Disabled until ?mcs is fixed
#	bug-331958.cs
TESTS_CS_SRC=		\
	generic-unloading-sub.2.cs	\
	create-instance.cs	\
	bug-2907.cs		\
	$(ARRAY_COOP_CS)	\
	array-12193.cs		\
	array-init.cs		\
	arraylist.cs		\
	assembly-load-remap.cs	\
	assembly-load-bytes.cs	\
	assembly-loadfile.cs	\
	assembly-loadfrom.cs	\
	assembly-load-bytes-bindingredirect.cs	\
	assembly-loadfile-bindingredirect.cs	\
	assembly-loadfrom-bindingredirect.cs	\
	assembly-loadfrom-simplename.cs	\
	assembly-refonly-load-bytes-bindingredirect.cs \
	assemblyresolve_event.cs	\
	assemblyresolve_event3.cs	\
	assemblyresolve_event4.cs	\
	assemblyresolve_event5.cs	\
	assemblyresolve_event6.cs	\
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
	exception-invokes.cs		\
	merp-json-valid.cs		\
	merp-crash-test.cs		\
	exception19.cs		\
	exception20.cs		\
	exception21.cs		\
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
	interface-2.cs		\
	dim-generic.cs		\
	dim-issue-18917.cs		\
	iface.cs		\
	iface2.cs		\
	iface3.cs		\
	iface4.cs		\
	iface-large.cs		\
	iface-contravariant1.cs \
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
	delegate14.cs		\
	delegate15.cs		\
	delegate16.cs		\
	delegate17.cs		\
	delegate18.cs		\
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
	threadpool-exceptions3.cs \
	threadpool-exceptions4.cs \
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
	delegate-disposed-hashcode.cs	\
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
	install_eh_callback.cs	\
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
	recursive-generics.3.cs	\
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
	custom-modifiers-inheritance.cs	\
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
	delegate-prop.cs	\
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
	abort-try-holes.cs \
	abort-tests.cs \
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
	appdomain-unload-asmload.cs	\
	appdomain-unload-exception.cs	\
	unload-appdomain-on-shutdown.cs	\
	appdomain-marshalbyref-assemblyload.cs \
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
	threadpool-exceptions8.cs \
	cross-domain.cs	\
	generic-unloading.2.cs	\
	namedmutex-destroy-race.cs	\
	thread6.cs	\
	thread7.cs	\
	appdomain-threadpool-unload.cs	\
	process-unref-race.cs	\
	bug-46661.cs	\
	w32message.cs \
	runtime-invoke.gen.cs \
	imt_big_iface_test.cs \
	bug-58782-plain-throw.cs \
	bug-58782-capture-and-throw.cs \
	recursive-struct-arrays.cs \
	struct-explicit-layout.cs \
	bug-59281.cs	\
	init_array_with_lazy_type.cs \
	weak-fields.cs \
	threads-leak.cs	\
	threads-init.cs \
	bug-60848.cs \
	bug-59400.cs \
	tailcall-generic-cast-cs.cs \
	tailcall-interface.cs \
	bug-60843.cs	\
	nested_type_visibility.cs	\
	dynamic-method-churn.cs	\
	dynamic-method-delegate.cs \
	verbose.cs \
	generic-unmanaged-constraint.cs \
	bug-10834.cs \
	bug-10837.cs	\
	bug-gh-9507.cs	\
	async-generic-enum.cs \
	null-blob-main.cs \
	last-error.cs \
	rgctx-thread-static.cs \
	ccw-class-iface.cs \
	bug-gh-17285.cs

# some tests fail to compile on mcs
if CSC_IS_ROSLYN
TESTS_CS_SRC += roslyn-bug-19038.cs sizeof-empty-structs.cs
endif

if AMD64
TESTS_CS_SRC += async-exc-compilation.cs finally_guard.cs finally_block_ending_in_dead_bb.cs
if !HOST_WIN32
# requires working altstack
TESTS_CS_SRC += bug-60862.cs
endif
endif

if X86
TESTS_CS_SRC += async-exc-compilation.cs finally_guard.cs finally_block_ending_in_dead_bb.cs
if !HOST_WIN32
# requires working altstack
TESTS_CS_SRC += bug-60862.cs
endif
endif

#maybe this test works on linux, not sure
if HOST_DARWIN
TESTS_CS_SRC += bug-8417.cs
endif

if CROSS_COMPILING # FIXME build vs. host vs. target
TAILCALL_INTERFACE_CONSERVESTACK_IL=
TAILCALL_FSHARP_DEEPTAIL_IL=
else
if HOST_WASM # FIXME build problem
TAILCALL_INTERFACE_CONSERVESTACK_IL=
TAILCALL_FSHARP_DEEPTAIL_IL=
else
if HOST_WIN32 # FIXME build problem
TAILCALL_INTERFACE_CONSERVESTACK_IL=
TAILCALL_FSHARP_DEEPTAIL_IL=
else
# These tests are produced by splitting up tailcall-interface-conservestack.il.
TAILCALL_INTERFACE_CONSERVESTACK_IL=\
	tailcall/interface-conservestack/1.il \
	tailcall/interface-conservestack/10.il \
	tailcall/interface-conservestack/11.il \
	tailcall/interface-conservestack/12.il \
	tailcall/interface-conservestack/13.il \
	tailcall/interface-conservestack/14.il \
	tailcall/interface-conservestack/15.il \
	tailcall/interface-conservestack/16.il \
	tailcall/interface-conservestack/17.il \
	tailcall/interface-conservestack/18.il \
	tailcall/interface-conservestack/19.il \
	tailcall/interface-conservestack/2.il \
	tailcall/interface-conservestack/20.il \
	tailcall/interface-conservestack/21.il \
	tailcall/interface-conservestack/22.il \
	tailcall/interface-conservestack/23.il \
	tailcall/interface-conservestack/24.il \
	tailcall/interface-conservestack/25.il \
	tailcall/interface-conservestack/26.il \
	tailcall/interface-conservestack/27.il \
	tailcall/interface-conservestack/28.il \
	tailcall/interface-conservestack/29.il \
	tailcall/interface-conservestack/3.il \
	tailcall/interface-conservestack/30.il \
	tailcall/interface-conservestack/31.il \
	tailcall/interface-conservestack/32.il \
	tailcall/interface-conservestack/33.il \
	tailcall/interface-conservestack/34.il \
	tailcall/interface-conservestack/35.il \
	tailcall/interface-conservestack/36.il \
	tailcall/interface-conservestack/37.il \
	tailcall/interface-conservestack/38.il \
	tailcall/interface-conservestack/39.il \
	tailcall/interface-conservestack/4.il \
	tailcall/interface-conservestack/40.il \
	tailcall/interface-conservestack/41.il \
	tailcall/interface-conservestack/42.il \
	tailcall/interface-conservestack/43.il \
	tailcall/interface-conservestack/44.il \
	tailcall/interface-conservestack/45.il \
	tailcall/interface-conservestack/46.il \
	tailcall/interface-conservestack/47.il \
	tailcall/interface-conservestack/48.il \
	tailcall/interface-conservestack/49.il \
	tailcall/interface-conservestack/5.il \
	tailcall/interface-conservestack/50.il \
	tailcall/interface-conservestack/51.il \
	tailcall/interface-conservestack/52.il \
	tailcall/interface-conservestack/53.il \
	tailcall/interface-conservestack/6.il \
	tailcall/interface-conservestack/7.il \
	tailcall/interface-conservestack/8.il \
	tailcall/interface-conservestack/9.il

# These tests are produced by splitting up tailcall/fsharp-deeptail.il.
TAILCALL_FSHARP_DEEPTAIL_IL=\
	tailcall/fsharp-deeptail/Seq.filter-length1.il \
	tailcall/fsharp-deeptail/Seq.filter-length2.il \
	tailcall/fsharp-deeptail/Seq.filter-length3.il \
	tailcall/fsharp-deeptail/StaticTailCallLoop_DateTime_.il \
	tailcall/fsharp-deeptail/StaticTailCallLoop_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DatstringeTime_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DatstringeTime_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_byte_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_string_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClass_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClass_int_.il \
	tailcall/fsharp-deeptail/TailCallLoopGenericClass_string_.il \
	tailcall/fsharp-deeptail/TailCallLoop_DateTime_.il \
	tailcall/fsharp-deeptail/TailCallLoop_int_.il \
	tailcall/fsharp-deeptail/mutualTail1IsEven.il \
	tailcall/fsharp-deeptail/mutualTail1IsOdd.il \
	tailcall/fsharp-deeptail/simpleTail1.il \
	tailcall/fsharp-deeptail/simpleTail2.il

$(TAILCALL_FSHARP_DEEPTAIL_IL): tailcall/fsharp-deeptail.stamp

$(TAILCALL_INTERFACE_CONSERVESTACK_IL): tailcall/interface-conservestack.stamp

tailcall/fsharp-deeptail.stamp: tailcall/split-fsharp$(EXEEXT) tailcall/fsharp-deeptail.il
	./tailcall/split-fsharp$(EXEEXT) < $(srcdir)/tailcall/fsharp-deeptail.il
	touch -f $@

tailcall/interface-conservestack.stamp: split-tailcall-interface-conservestack$(EXEEXT) tailcall-interface-conservestack.il
	./split-tailcall-interface-conservestack$(EXEEXT) < $(srcdir)/tailcall-interface-conservestack.il
	touch -f $@

noinst_PROGRAMS = tailcall/split-fsharp split-tailcall-interface-conservestack

tailcall_split_fsharp_SOURCES=tailcall/split-fsharp.cpp

split_tailcall_interface_conservestack_SOURCES=split-tailcall-interface-conservestack.cpp

endif # HOST_WIN32
endif # HOST_WASM
endif # CROSS_COMPILING

BUILT_SOURCES += $(TAILCALL_FSHARP_DEEPTAIL_IL) $(TAILCALL_INTERFACE_CONSERVESTACK_IL)

TESTS_IL_SRC=			\
	tailcall/2.il	     \
	tailcall/3.il	     \
	tailcall/4.il	     \
	tailcall/fsharp-deeptail.il	     \
	tailcall/fsharp-shallowtail.il	 \
	tailcall/fsharp-shallownotail.il \
	$(TAILCALL_FSHARP_DEEPTAIL_IL) \
	gptail1.il		\
	itail1.il		\
	itaili1.il		\
	ivtail1.il		\
	sirtail1.il		\
	sitail1.il		\
	srtail1.il		\
	stail1.il		\
	tail1.il		\
	taili1.il		\
	vtail1.il		\
	tailcall-virt.il 	\
	tailcall-rgctxa.il 	\
	tailcall-rgctxb.il 	\
	tailcall-rgctxb-static.il \
	tailcall-mrgctx.il 	\
	$(TAILCALL_INTERFACE_CONSERVESTACK_IL) \
	tailcall-interface-justrun.il \
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
	vararg3.il		\
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
	calli_sig_check.il	\
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
	instance_tailrec.il	\
	dim-constrainedcall.il	\
	dim-constrained3.il	\
	dim-constrained3_gm.il	\
	dim-diamondshape.il	\
	dim-genericmethods.il	\
	dim-methodimpl.il	\
	dim-sharedgenerics.il	\
	dim-simple.il	\
	dim-valuetypes.il \
	dim-nested-types.il \
	dim-generic-interface.il \
	dim-implements-multiple-interfaces.il \
	dim-sealed.il \
	dim-protected-accessibility1.il \
	dim-protected-accessibility2.il \
	dim-reabstraction.il \
	dim-reabstraction-generics.il \
	dim-abstractcall.il \
	twopassvariance.il \
	tailcall-generic-cast-conservestack-il.il \
	tailcall-generic-cast-nocrash-il.il \
	tailcall-member-function-in-valuetype.il \
	tailcall-return-valuetype.il \
	tailcall-valuetype-parameter.il \
	ldfldvt.il \
	newobj-abstract.il \
	invalid-isbyreflike.il \
	custom-modifiers-append.1.il \
	gh-13056_mono_local_cprop_av.il \
	gh-13057_mono_local_emulate_ops_av.il \
	module-cctor-entrypoint.il \
	bug-gh-9706.il \
	localloc-noinit.il \
	calli_native.il

# This test crashes the runtime, even with recent fixes.
#	incorrect-ldvirtftn-read-behind-for-dup.il

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

PLATFORM_DISABLED_TESTS=\
	$(TAILCALL_DISABLED_TESTS_RUN) \
	tailcall-member-function-in-valuetype.exe \
	tailcall-rgctxb.exe

if HOST_WIN32
PLATFORM_DISABLED_TESTS += bug-58782-plain-throw.exe bug-58782-capture-and-throw.exe

# see https://github.com/mono/mono/issues/9739
PLATFORM_DISABLED_TESTS += verbose.exe

if ENABLE_CXX
# see https://github.com/mono/mono/issues/18827
PLATFORM_DISABLED_TESTS += bug-10127.exe
endif

endif

if DISABLE_CRASH_REPORTING
PLATFORM_DISABLED_TESTS += merp-json-valid.exe merp-crash-test.exe
endif

if HOST_DARWIN

# see https://github.com/mono/mono/issues/10845
PLATFORM_DISABLED_TESTS += monitor-wait-abort.exe
else
PLATFORM_DISABLED_TESTS += merp-json-valid.exe merp-crash-test.exe
endif

if AMD64
# #651684
PLATFORM_DISABLED_TESTS += finally_guard.exe

if HOST_WIN32
PLATFORM_DISABLED_TESTS += w32message.exe
endif

endif

if X86

if HOST_WIN32
PLATFORM_DISABLED_TESTS += async-exc-compilation.exe finally_guard.exe finally_block_ending_in_dead_bb.exe \
	bug-18026.exe monitor.exe threadpool-exceptions5.exe process-unref-race.exe w32message.exe \
	unhandled-exception-1.exe unhandled-exception-2.exe unhandled-exception-3.exe unhandled-exception-4.exe \
	unhandled-exception-5.exe unhandled-exception-6.exe unhandled-exception-7.exe unhandled-exception-8.exe
endif

# test_virt.exe fails because callee takes more parameters than caller, 1 vs. 0, and
# x86 passes no parameters in registers, so it is a stack imbalance.
PLATFORM_DISABLED_TESTS += tailcall/coreclr/JIT/Methodical/tailcall/test_virt.exe

endif

if POWERPC
# bug #71274
PLATFORM_DISABLED_TESTS += finalizer-abort.exe finalizer-exception.exe finalizer-exit.exe
endif

if POWERPC64
# FIXME: These tests hang/fail for unknown reasons
PLATFORM_DISABLED_TESTS += monitor.exe threadpool-exceptions5.exe appdomain-unload.exe \
	pinvoke2.exe pinvoke3.exe pinvoke11.exe threadpool-exceptions7.exe winx64structs.exe bug-10127.exe pinvoke_ppcc.exe \
	pinvoke_ppcs.exe pinvoke_ppci.exe pinvoke_ppcf.exe pinvoke_ppcd.exe abort-cctor.exe load-exceptions.exe \
	sgen-domain-unload-2.exe sgen-weakref-stress.exe sgen-cementing-stress.exe sgen-new-threads-dont-join-stw.exe \
	sgen-new-threads-dont-join-stw-2.exe sgen-new-threads-collect.exe sgen-bridge.exe
PLATFORM_DISABLED_TESTS += appdomain-threadpool-unload.exe
PLATFORM_DISABLED_TESTS += bug-60848.exe
endif

if ARM
PLATFORM_DISABLED_TESTS += filter-stack.exe weak-fields.exe

# Most ARM problems are due to extra_arg and cannot be easily fixed, except for easy
# bitcode-specific fix which has been declined. A few are uncertain and should be investigated,
# but still probably extra_arg.
PLATFORM_DISABLED_TESTS += ivtail1.exe
PLATFORM_DISABLED_TESTS += tailcall-generic-cast-conservestack-il.exe
PLATFORM_DISABLED_TESTS += tailcall-mrgctx.exe
PLATFORM_DISABLED_TESTS += tailcall-rgctxa.exe
PLATFORM_DISABLED_TESTS += tailcall-virt.exe
PLATFORM_DISABLED_TESTS += tailcall/coreclr/JIT/Directed/tailcall/tailcall.exe
PLATFORM_DISABLED_TESTS += tailcall/2.exe
PLATFORM_DISABLED_TESTS += tailcall/3.exe
PLATFORM_DISABLED_TESTS += tailcall/4.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_string_.exe

# FIXME These presumably take an extra parameter though this should be confirmed.
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_int_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DatstringeTime_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_int_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_int_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_int_.exe

# Interface calls cannot be tailcalls on ARM due to extra_arg. Bitcode could easily work however.
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_int_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DatstringeTime_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_int_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_int_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_string_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_DateTime_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_byte_.exe
PLATFORM_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_int_.exe

# Interface calls cannot be tailcalls on ARM due to extra_arg. Bitcode could easily work however.
PLATFORM_DISABLED_TESTS += tailcall-interface-conservestack.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/1.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/10.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/11.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/12.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/13.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/14.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/15.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/16.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/17.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/18.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/19.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/2.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/20.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/21.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/22.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/23.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/24.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/25.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/26.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/27.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/28.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/29.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/3.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/30.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/31.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/32.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/33.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/34.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/35.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/36.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/37.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/38.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/39.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/4.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/40.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/41.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/42.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/43.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/44.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/45.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/46.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/47.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/48.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/49.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/5.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/50.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/51.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/52.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/6.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/7.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/8.exe
PLATFORM_DISABLED_TESTS += tailcall/interface-conservestack/9.exe
endif

if ARM64
PLATFORM_DISABLED_TESTS += weak-fields.exe
endif

if MIPS
# monitor.exe is racy
PLATFORM_DISABLED_TESTS += filter-stack.exe monitor.exe
endif

if S390X
PLATFORM_DISABLED_TESTS += dynamic-method-resurrection.exe
#PLATFORM_DISABLED_TESTS=dynamic-method-resurrection.exe exception17.exe

PLATFORM_DISABLED_TESTS += \
	sgen-toggleref.exe \
	sgen-bridge.exe \
	sgen-bridge-major-fragmentation.exe \
	sgen-bridge-xref.exe
endif

PROFILE_DISABLED_TESTS= \
	$(TAILCALL_DISABLED_TESTS_RUN) \
	tailcall-member-function-in-valuetype.exe \
	tailcall-rgctxb.exe

if FULL_AOT_TESTS
# Tests which rely on TypeLoadExceptions or MemberAccessExceptions
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
	custom-attr-errors.exe \
	ldfld_missing_field.exe \
	ldfld_missing_class.exe \
	vt-sync-method.exe \
	resolve_method_bug.2.exe \
	resolve_field_bug.2.exe \
	resolve_type_bug.2.exe \
	bug-81691.exe \
	bug-327438.2.exe \
	newobj-abstract.exe \
	invalid-isbyreflike.exe

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
	thunks.exe \
	bug-58782-plain-throw.exe \
	bug-58782-capture-and-throw.exe

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
	appdomain-unload-asmload.exe \
	appdomain-unload-callback.exe	\
	appdomain-unload-doesnot-raise-pending-events.exe	\
	unload-appdomain-on-shutdown.exe	\
	appdomain-marshalbyref-assemblyload.exe \
	assemblyresolve_event2.2.exe	\
	assemblyresolve_event6.exe	\
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
	threadpool-exceptions8.exe	\
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
	custom-modifiers-inheritance.exe	\
	bug-462592.exe	\
	bug-575941.exe	\
	bug-389886-3.exe	\
	constant-division.exe	\
	dynamic-method-resurrection.exe	\
	assembly_append_ordering.exe \
	assemblyresolve_event5.exe	\
	dynamic-method-delegate.exe \
	dynamic-method-churn.exe \
	bug-666008.exe \
	ccw-class-iface.exe \
	bug-685908.exe

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

# Tests which have no way of finding AOT images because they use Assembly.Load(byte[])
PROFILE_DISABLED_TESTS += \
	assembly-load-bytes.exe \
	assembly-load-bytes-bindingredirect.exe

# Tests which require AOT of a multi-netmodule assemblies
PROFILE_DISABLED_TESTS += test-multi-netmodule-4-exe.exe

# Tests which depend on a case-insensitive filesystem when using AOT
if HOST_DARWIN
else
PROFILE_DISABLED_TESTS += \
	assembly-loadfrom-simplename.exe
endif

# constraints-load.il: 
# Failed to load method 0x6000007 from '..../mono/tests/constraints-load.exe' due to 
# Could not resolve type with token 01000002 assembly:mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089 type:System.BrokenIComparable`1 member:<none>.
PROFILE_DISABLED_TESTS += \
	constraints-load.exe \
	calli_sig_check.exe \
	bug-515884.exe

PROFILE_DISABLED_TESTS += \
	generic-type-builder.2.exe

PROFILE_DISABLED_TESTS += \
	sgen-domain-unload.exe	\
	sgen-domain-unload-2.exe

PROFILE_DISABLED_TESTS += \
	appdomain-loader.exe \
	assemblyresolve_event3.exe \
	appdomain-serialize-exception.exe

PROFILE_DISABLED_TESTS += \
	delegate15.exe

# https://bugzilla.xamarin.com/show_bug.cgi?id=60973
PROFILE_DISABLED_TESTS += \
	weak-fields.exe

# Some, probably all, of these tests use gsharedvt.
# gsharedvt is very much at odds with tailcall, and they fail as a result.
# RegularAOT and HybridAOT should be ok.
#
# The tests might be tweakable but then they will less resemble real world code.
#
# FullAOT should probably be changed to intersect tailcall signatures called
# and all generic signatures provided, and compile more functions.
# As well, gsharedvt can probably be a tailcall sometimes, like if parameters
# fit in registers and there is no out conversion. Maybe it already does that.
#
# gsharedvt should be viable when only wrapping "in" but not "out",
# as long as "in" does not expand parameter stack size, which it should never do.
if MONO_ARCH_GSHAREDVT_SUPPORTED
PROFILE_DISABLED_TESTS += tailcall-interface-conservestack.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/20.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/26.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/31.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/33.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/34.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/35.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/36.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/37.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/41.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/43.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/44.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/45.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/46.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/47.exe
PROFILE_DISABLED_TESTS += tailcall/interface-conservestack/51.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_string_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_string_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_string_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_string_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_byte_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_DateTime_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_int_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_string_.exe
PROFILE_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_string_.exe

endif # MONO_ARCH_GSHAREDVT_SUPPORTED

if ARM64
# Times out on ARM64.
# https://github.com/mono/mono/issues/10783
# https://jenkins.mono-project.com/job/test-mono-pull-request-arm64-fullaot+llvm/486/parsed_console/log.html
# https://jenkins.mono-project.com/job/test-mono-pull-request-arm64-fullaot+llvm/471/parsed_console/log.html
PLATFORM_DISABLED_TESTS += tailcall/2.exe
endif # ARM64 FullAOT

endif # FULL_AOT_TESTS

if HYBRID_AOT_TESTS
PROFILE_DISABLED_TESTS += \
	bug-80307.exe \
	namedmutex-destroy-race.exe
endif

if FULL_AOT_INTERP_TESTS

# AppDomain tests
PROFILE_DISABLED_TESTS += \
	appdomain1.exe \
	appdomain2.exe \
	appdomain-async-invoke.exe \
	appdomain.exe \
	appdomain-exit.exe \
	appdomain-loader.exe \
	appdomain-marshalbyref-assemblyload.exe \
	appdomain-serialize-exception.exe \
	appdomain-thread-abort.exe \
	appdomain-threadpool-unload.exe \
	appdomain-unload-asmload.exe \
	appdomain-unload-callback.exe \
	appdomain-unload-doesnot-raise-pending-events.exe \
	appdomain-unload.exe \
	assemblyresolve_event6.exe \
	bug-335131.2.exe \
	bug-415577.exe \
	cross-domain.exe \
	dataslot.exe \
	delegate9.exe \
	domain-stress.exe \
	generic-marshalbyref.2.exe \
	generic-unloading.2.exe \
	generic-xdomain.2.exe \
	hostname.exe \
	marshal-valuetypes.exe \
	monitor.exe \
	pinvoke3.exe \
	remoting4.exe \
	stackframes-async.2.exe \
	suspend-stress-test.exe \
	thread6.exe \
	threadpool-exceptions7.exe \
	threadpool-exceptions8.exe	\
	unhandled-exception-7.exe \
	unhandled-exception-test-case.2.exe \
	unload-appdomain-on-shutdown.exe \
	xdomain-threads.exe

# Tests which load assemblies which are not in the profile
PROFILE_DISABLED_TESTS += \
	assembly-load-remap.exe

# Tests which needs named Mutex support
PROFILE_DISABLED_TESTS += \
	namedmutex-destroy-race.exe

# Test which needs System.Web support
PROFILE_DISABLED_TESTS += \
	bug-80307.exe

PROFILE_DISABLED_TESTS += \
	transparentproxy.exe

# Tests depending on assembly load order. Under Full AOT using WinAOT
# profile, console is still available, meaning that more assemblies (I18N)
# will be loaded when AOT runtime initial loads mscorlib.
if HOST_WIN32
PROFILE_DISABLED_TESTS += \
	assembly_append_ordering.exe
endif
endif

REMOTING_DISABLED_TESTS = \
	async-exc-compilation.exe \
	bug-544446.exe \
	context-static.exe \
	remoting1.exe \
	remoting2.exe \
	remoting3.exe \
	remoting5.exe \
	nullable_boxing.2.exe

if FULL_AOT_TESTS
PROFILE_DISABLED_TESTS += $(REMOTING_DISABLED_TESTS)

# Test is too sensitive.
PROFILE_DISABLED_TESTS += localloc-noinit.exe
endif

if HYBRID_AOT_TESTS
PROFILE_DISABLED_TESTS += $(REMOTING_DISABLED_TESTS)

# Test is too sensitive.
PROFILE_DISABLED_TESTS += localloc-noinit.exe
endif

if FULL_AOT_INTERP_TESTS
PROFILE_DISABLED_TESTS += $(REMOTING_DISABLED_TESTS)

# Test is too sensitive.
PROFILE_DISABLED_TESTS += localloc-noinit.exe

endif

AOT_DISABLED_TESTS= \
	localloc-noinit.exe \
	constraints-load.exe \
	calli_sig_check.exe

CI_DISABLED_TESTS = \
	main-returns-background-resetabort.exe \
	main-returns-background-abort-resetabort.exe	\
	assemblyresolve_event3.exe \
	finally_guard.exe \
	generic-xdomain.2.exe

# failing tests which we temporarily disable for PRs
# so they don't interfere with other people's work
CI_PR_DISABLED_TESTS =

# FIXME Hybrid Suspend - see https://github.com/mono/mono/issues/9959
# CI_PR_DISABLED_TESTS += sgen-new-threads-collect.exe

# https://github.com/mono/mono/issues/8997
CI_PR_DISABLED_TESTS += finalizer-thread.exe

# https://github.com/mono/mono/issues/11392
CI_PR_DISABLED_TESTS += appdomain-threadpool-unload.exe

# https://github.com/mono/mono/issues/11415
CI_PR_DISABLED_TESTS += finalizer-abort.exe

# https://github.com/mono/mono/issues/11420
CI_PR_DISABLED_TESTS += process-stress-2.exe

# https://github.com/mono/mono/issues/11417
CI_PR_DISABLED_TESTS += process-stress-3.exe

if HOST_WIN32
# https://github.com/mono/mono/issues/12839
CI_PR_DISABLED_TESTS += block_guard_restore_aligment_on_exit.exe
endif

# appdomain-threadpool-unload.exe creates 100 appdomains, takes too long with llvm
LLVM_DISABLED_TESTS = \
	finally_block_ending_in_dead_bb.exe \
	appdomain-threadpool-unload.exe

# https://github.com/mono/mono/issues/6490
LLVM_DISABLED_TESTS += \
	weak-fields.exe

if HOST_WIN32
# https://github.com/mono/mono/issues/19603
LLVM_DISABLED_TESTS += sgen-bridge.exe
endif

LLVM = $(filter --llvm, $(MONO_ENV_OPTIONS) $(AOT_BUILD_FLAGS))

# The two finalizer tests only work under sgen
# gc-altstack.exe fails under boehm because it has no support for altstack
# bug-459094.exe creates an extremely deep directory tree
# delegate-invoke.exe depends on 929c6bc9b6d76a273f251e6f5dfacac36e9c38bd which was
# reverted.
# bug-Xamarin-5278.exe got broken by 5d26590e79da139a284459299aee95c25f4cd835
# appdomain-thread-abort.exe: https://bugzilla.xamarin.com/show_bug.cgi?id=47054
# abort-try-holes.exe is flaky due to unwinding failure to the finally block when aborting
# appdomain-marshalbyref-assemblyload.exe: https://bugzilla.xamarin.com/show_bug.cgi?id=49308
# threads-init.exe: runs out of system threads
# dim-constrainedcall.exe: fails on dontnet as well (https://github.com/dotnet/coreclr/issues/15353)
#
# tailcall-rgctxb.exe
# tailcall-member-function-in-valuetype.exe
#	Any time we have a byref/out parameter, be it
#	an integer, or valuetype-this, we pessimistically
#	will never tailcall.
#
#	These two cases have valuetype-this.
#	tailcall-rgctxb.exe strives for additional coverage
#	related to generic context, but both tests suffer from
#	the same problem.
#
#	I suggest we insert range checks.
#	If all parameters are outside the frame,
#	do a tailcall. If any are not, regular call.
#
#	It'll be a little bigger and have a slowdown,
#	and a speedup (stack locality speeds things up),
#	and enable code that does not work otherwise.
#
#	Again, tailcall is not just an optimization.
#	It lets code process large data within small stack.
#	If it was just an optimization, desktop CLR would not
#	go so far out of their way to implement it.
#
# Some of the tailcall tests pass native pointers to locals.
#   Mono does not yet allow that.
#   tailcall "i" is either calli, or integer instead of pointer, and the latter sidesteps the matter.
KNOWN_FAILING_TESTS = \
	delegate-async-exception.exe	\
	bug-348522.2.exe	\
	bug-459094.exe \
	delegate-invoke.exe \
	bug-Xamarin-5278.exe \
	appdomain-marshalbyref-assemblyload.exe	\
	abort-try-holes.exe \
	threads-init.exe \
	gptail1.exe \
	itaili1.exe \
	sirtail1.exe \
	srtail1.exe \
	stail1.exe \
	tail1.exe \
	taili1.exe \
	vtail1.exe \
	tailcall-member-function-in-valuetype.exe \
	tailcall-rgctxb.exe

DISABLED_TESTS = \
	$(KNOWN_FAILING_TESTS) \
	$(PLATFORM_DISABLED_TESTS) \
	$(PROFILE_DISABLED_TESTS) \
	$(if $(AOT),$(AOT_DISABLED_TESTS)) \
	$(if $(CI),$(CI_DISABLED_TESTS)) \
	$(if $(CI_PR),$(CI_PR_DISABLED_TESTS)) \
	$(if $(LLVM),$(LLVM_DISABLED_TESTS))

# Interpreter does not currently implement tailcall.
# Disable all tailcall tests.
INTERP_DISABLED_TESTS = $(DISABLED_TESTS) \
	gptail1.exe \
	itail1.exe \
	itaili1.exe \
	ivtail1.exe \
	sitail1.exe \
	sirtail1.exe \
	srtail1.exe \
	stail1.exe \
	tail1.exe \
	taili1.exe \
	vtail1.exe \
	tailcall-generic-cast-conservestack-il.exe \
	tailcall-interface-conservestack.exe \
	tailcall-mrgctx.exe \
	tailcall-rgctxa.exe \
	tailcall-rgctxb.exe \
	tailcall-rgctxb-static.exe \
	tailcall-virt.exe \
	tailcall-return-valuetype.exe \
	tailcall/fsharp-deeptail.exe \
	tailcall/2.exe \
	tailcall/3.exe \
	tailcall/4.exe \
	tailcall/8273.exe \
	$(TAILCALL_DISABLED_TESTS_RUN) \
	bug-60843.exe
# FIXME
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/Seq.filter-length1.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/Seq.filter-length2.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/Seq.filter-length3.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/StaticTailCallLoop_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/StaticTailCallLoop_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DateTime_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_DatstringeTime_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_byte_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_int_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractClass_string_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DateTime_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_DatstringeTime_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_byte_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_int_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethodAbstractInterface_string_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_DateTime_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_byte_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_int_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_byte_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClassAndMethod_string_.Method1_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClass_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClass_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoopGenericClass_string_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoop_DateTime_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/TailCallLoop_int_.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/mutualTail1IsEven.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/mutualTail1IsOdd.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/simpleTail1.exe
INTERP_DISABLED_TESTS += tailcall/fsharp-deeptail/simpleTail2.exe

INTERP_DISABLED_TESTS += tailcall/interface-conservestack/1.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/10.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/11.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/12.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/13.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/14.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/15.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/16.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/17.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/18.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/19.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/2.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/20.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/21.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/22.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/23.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/24.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/25.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/26.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/27.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/28.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/29.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/3.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/30.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/31.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/32.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/33.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/34.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/35.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/36.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/37.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/38.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/39.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/4.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/40.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/41.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/42.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/43.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/44.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/45.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/46.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/47.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/48.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/49.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/5.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/50.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/51.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/52.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/53.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/6.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/7.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/8.exe
INTERP_DISABLED_TESTS += tailcall/interface-conservestack/9.exe

# expected InvalidProgramException not thrown
INTERP_DISABLED_TESTS += calli_sig_check.exe

# Test is too sensitive.
INTERP_DISABLED_TESTS += localloc-noinit.exe
INTERP_DISABLED_TESTS += dim-diamondshape.exe
INTERP_DISABLED_TESTS += pinvoke3.exe
INTERP_DISABLED_TESTS += cominterop.exe
INTERP_DISABLED_TESTS += ccw-class-iface.exe
# bug-60862.exe: missing support to map IP->method; only works on platforms with altstack support.
INTERP_DISABLED_TESTS += bug-60862.exe
# bug-48015.exe: remoting test that fails on fullaotinterp scenarios
INTERP_DISABLED_TESTS += bug-48015.exe

if ARM64
INTERP_DISABLED_TESTS += \
	bug-80307.exe

# Test is too sensitive.
DISABLED_TESTS += localloc-noinit.exe

endif

if HOST_WIN32
# Test is too sensitive?
DISABLED_TESTS += localloc-noinit.exe
endif

TESTS_CS=$(filter-out $(DISABLED_TESTS),$(TESTS_CS_SRC:.cs=.exe))
TESTS_IL=$(filter-out $(DISABLED_TESTS),$(TESTS_IL_SRC:.il=.exe))
TESTS_BENCH=$(filter-out $(DISABLED_TESTS),$(TESTS_BENCH_SRC:.cs=.exe))
TESTS_STRESS=$(filter-out $(DISABLED_TESTS),$(TESTS_STRESS_SRC:.cs=.exe))
TESTS_GSHARED=$(filter-out $(DISABLED_TESTS),$(TESTS_GSHARED_SRC:.cs=.exe))
TESTS_TAILCALL_COMPILE=$(filter-out $(TAILCALL_DISABLED_TESTS_COMPILE),$(TEST_TAILCALL_CS_SRC:.cs=.exe) $(TEST_TAILCALL_IL_SRC:.il=.exe))
TESTS_TAILCALL_RUN=$(filter-out $(TAILCALL_DISABLED_TESTS_RUN),$(TESTS_TAILCALL_COMPILE))

if FULL_AOT_TESTS
TESTSAOT_CS=$(TESTS_CS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_IL=$(TESTS_IL:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_BENCH=$(TESTS_BENCH:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_STRESS=$(TESTS_STRESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_GSHARED=$(TESTS_GSHARED:.exe=.exe$(PLATFORM_AOT_SUFFIX))
# AOT tests are run by running the non-AOT files. Runtime probes for the AOT file.
TESTSAOT_TAILCALL=$(TESTS_TAILCALL_COMPILE:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_CS=$(TESTS_CS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_IL=$(TESTS_IL:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_BENCH=$(TESTS_BENCH:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_STRESS=$(TESTS_STRESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_GSHARED=$(TESTS_GSHARED:.exe=.exe$(PLATFORM_AOT_SUFFIX))
TESTSAOT_TAILCALL=$(TESTS_TAILCALL_COMPILE:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

EXTRA_DIST=common_mixed.mk test-driver test-runner.cs $(TESTS_CS_SRC) $(TESTS_IL_SRC) \
	$(TESTS_BENCH_SRC) $(TESTS_STRESS_SRC) stress-runner.pl \
	array-coop-1.cs array-coop-2.cs \
	array-coop-bigvt.cs array-coop-int.cs array-coop-smallvt.cs \
	array-coop-bigvt.sh array-coop-smallvt.sh array-coop-int.sh \
	tailcall-interface-conservestack.il tailcall/fsharp-deeptail.il \
	$(TEST_TAILCALL_CS_SRC) \
	$(TEST_TAILCALL_IL_SRC)

# This test has extra sources.
tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyWithPrefix.exe: \
		tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyTransparentLibraryWithPrefix.il \
		tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyVerifiableLibraryWithPrefix.il

%.exe: %.il
	$(MKDIR_P) $(dir $@)
	$(ILASM) -out:$@ $^

if !FULL_AOT_TESTS
if !HYBRID_AOT_TESTS
if !FULL_AOT_INTERP_TESTS
TEST_DRIVER_HARD_KILL_FEATURE=-r:$(CLASS)/Mono.Posix.dll
endif
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
	$(MKDIR_P) $(dir $@)
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -r:TestDriver.dll $(TEST_DRIVER_HARD_KILL_FEATURE) -out:$@ $<

# N.B. test-runner.exe references the TOOLS_RUNTIME versions of the framework assemblies
test-runner.exe: test-runner.cs $(TEST_DRIVER_DEPEND)
	$(MCS) -r:System.dll -r:System.Xml.dll -r:System.Core.dll -r:TestDriver.dll $(TEST_DRIVER_HARD_KILL_FEATURE) -out:$@ $<

%.dll: %.cs
	$(MKDIR_P) $(dir $@)
	$(MCS) -r:$(CLASS)/System.dll -target:library -out:$@ $<
%.dll: %.il
	$(MKDIR_P) $(dir $@)
	$(ILASM) /dll /output:$@ $<

reference-loader.exe: reference-loader.cs TestingReferenceAssembly.dll TestingReferenceReferenceAssembly.dll $(TEST_DRIVER_DEPEND)
	$(MCS) -r:$(CLASS)/System.dll -r:TestDriver.dll -r:TestingReferenceAssembly.dll -r:TestingReferenceReferenceAssembly.dll $(TEST_DRIVER_HARD_KILL_FEATURE) -out:$@ $(srcdir)/reference-loader.cs

merp-json-valid.exe: merp-json-valid.cs
	$(MCS) -r:$(CLASS)/System.Web.Extensions.dll -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -out:$@ $<

merp-crash-test.exe: merp-crash-test.cs
	$(MCS) -r:$(CLASS)/System.Web.Extensions.dll -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -out:$@ $<

TestingReferenceAssembly.dll: TestingReferenceAssembly.cs
	$(MCS) -target:library -out:$@ $<

TestingReferenceReferenceAssembly.dll: TestingReferenceReferenceAssembly.cs TestingReferenceAssembly.dll
	$(MCS) -r:TestingReferenceAssembly.dll -target:library -out:$@ $<

%.exe$(PLATFORM_AOT_SUFFIX): %.exe 
	$(RUNTIME) $(TEST_AOT_BUILD_FLAGS) $<

%.dll$(PLATFORM_AOT_SUFFIX): %.dll 
	$(RUNTIME) $(TEST_AOT_BUILD_FLAGS) $<

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

#
# Test that no symbols are missed in eglib-remap.h
#
if HOST_LINUX
test-platform: test-eglib-remap
else
if HOST_DARWIN
test-platform: test-eglib-remap
endif
endif
# The following regexp describes all symbols that start with "g_" but are not part of eglibc.
# The optional underscore prepending symbol names may or may not appear depending on the
# system and the state of the leading-underscore compiler flag.
OK_G_SYMBOLS='\<_?g_(s?list_(pre|ap)pend_(image|mempool)|concat_dir_and_file|Ctoc)\>'
test-eglib-remap:
	@echo "Testing eglib remap..."
	@if which nm > /dev/null; then if nm $(top_builddir)/mono/mini/mono | grep -Ev $(OK_G_SYMBOLS) | grep -E 't _?g_'; then exit 1; else exit 0; fi; fi

test-env-options:
	MONO_ENV_OPTIONS="--version" $(RUNTIME) array-init.exe | grep -q Architecture:

TESTS_REGULAR = $(TESTS_CS) $(TESTS_IL) $(TESTS_BENCH)
TESTS_INCL_DEPS = $(shell find . -type f -name "*.exe" -o -name "*.dll" -o -name "*.netmodule" -o -name "*.config")

# Target to precompile the test executables
test-local: $(TESTS_REGULAR) $(TESTS_STRESS) $(TESTS_GSHARED) $(TESTSAOT_CS) $(TESTSAOT_IL) $(TESTSAOT_BENCH) $(TESTSAOT_STRESS) $(TESTSAOT_GSHARED) $(TESTS_TAILCALL) $(TESTSAOT_TAILCALL) compile-gac-loading compile-assembly-load-reference test-runner.exe

test-bundle-local:
	mkdir -p $(addprefix $(TEST_BUNDLE_PATH)/tests/runtime/,$(sort $(dir $(TESTS_INCL_DEPS))))
	@echo $(TESTS_REGULAR) > $(TEST_BUNDLE_PATH)/tests/runtime/runtime-test-list.txt
	sed -e 's,$$mono_libdir,$$test_root_dir,g' tests-config > $(TEST_BUNDLE_PATH)/tests/runtime/tests-config.tmpl
	cp -L .libs/libtest$(libsuffix) $(TEST_BUNDLE_PATH)/tests/runtime/
	$(foreach asset,$(TESTS_INCL_DEPS),cp -L $(asset) $(TEST_BUNDLE_PATH)/tests/runtime/$(dir $(asset));)

# Precompile the test assemblies in parallel
compile-tests:
	$(MAKE) -j4 test

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
	for i in $(TESTS_REGULAR); do echo $${i} >> testlist; sort testlist > testlist.sorted; done; \
	for i in `cat testlist.sorted`; do \
		rm -f $${i}.so; \
		$(with_mono_path) $(JITTEST_PROG_RUN) --aot $(TEST_AOT_BUILD_FLAGS) --debug $${i} > $${i}.aotlog 2>&1 || exit 1; \
		if $(srcdir)/test-driver '$(with_mono_path) $(JITTEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" "$${dump_action}" $(TEST_RUNTIME_ARGS) $(TEST_AOT_RUN_FLAGS) ; \
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
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j a --testsuite-name "runtime" --timeout 300 --disabled "$(DISABLED_TESTS)" $(TESTS_REGULAR)

runtest-managed-serial: test-runner.exe compile-tests
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j 1 --testsuite-name "runtime" --disabled "$(DISABLED_TESTS)" $(TESTS_REGULAR)

test-jit:
	@if test x$(M) != x0; then $(MAKE) runtest-managed; else $(MAKE) runtest; fi

testaot:
	@$(MAKE) AOT=1 runtest

testtrace:
	@$(MAKE) TEST_RUNTIME_ARGS="$${TEST_RUNTIME_ARGS} --trace" runtest

testinterp: test-runner.exe compile-tests
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j a --runtime-args "--interpreter" --testsuite-name "runtime-interp" --timeout 300 --disabled "$(INTERP_DISABLED_TESTS)" $(TESTS_REGULAR)

testfullaotinterp: test-runner.exe compile-tests
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j a --runtime-args "--full-aot-interp" --testsuite-name "runtime-aot-interp" --timeout 300 --disabled "$(INTERP_DISABLED_TESTS)" $(TESTS_REGULAR)

testllvmonlyinterp: test-runner.exe compile-tests
	$(TOOLS_RUNTIME) --debug $(TEST_RUNNER) -j a --runtime-args "--llvmonly-interp" --testsuite-name "runtime-aot-interp" --timeout 300 --disabled "$(INTERP_DISABLED_TESTS)" $(TESTS_REGULAR)

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
		if $(srcdir)/stress-runner.pl $$i ../mini/mono $(TEST_RUNTIME_ARGS); \
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
	$(Q) $(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --disabled "$(DISABLED_TESTS)" $<

EXTRA_DIST += custom-attr-errors.cs custom-attr-errors-lib.cs

custom-attr-errors.exe custom-attr-errors-lib.dll: custom-attr-errors.cs custom-attr-errors-lib.cs $(TEST_DRIVER_DEPEND)
	$(Q) $(MCS) /t:library -D:WITH_MEMBERS custom-attr-errors-lib.cs
	$(Q) $(MCS) -r:TestDriver.dll -r:custom-attr-errors-lib.dll custom-attr-errors.cs
	$(Q) $(MCS) /t:library custom-attr-errors-lib.cs

test-cattr-type-load: custom-attr-errors.exe
	$(Q) $(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --disabled "$(DISABLED_TESTS)" $<

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

# The .cs tests are not interesting initially but should be later.

#FIXME
#coreclr/tailcall/CoreMangLib/system/span/SlowTailCallArgs.cs(124,13): error CS8107: Feature 'ref structs' is not available in C# 7.0. Please use language version 7.2 or greater.
#coreclr/tailcall/CoreMangLib/cti/system/reflection/emit/opcodes/opcodestailcall.cs(17,9): error CS0103: The name 'TestLibrary' does not exist in the current context

TEST_TAILCALL_CS_SRC=\
	tailcall/coreclr/JIT/CodeGenBringUpTests/RecursiveTailCall.cs \
	tailcall/coreclr/JIT/Methodical/tailcall/Desktop/thread-race.cs \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/delegateParamCallTarget.cs \
	tailcall/coreclr/JIT/opt/FastTailCall/FastTailCallCandidates.cs \
	tailcall/coreclr/JIT/opt/FastTailCall/FastTailCallInlining.cs \
	tailcall/coreclr/JIT/opt/FastTailCall/GitHubIssue12479.cs \
	tailcall/coreclr/JIT/opt/FastTailCall/StackFixup.cs \
	tailcall/coreclr/JIT/opt/FastTailCall/StructPassingSimple.cs

# At these locations we have .cs and .il.
# The .il is clearly derived from the .cs, such as by running
# csc + ildasm + edit.
# The .cs is opportunistically optimizable, see https://github.com/mono/mono/issues/7259.
# But for now we only want the .il.
#tailcall/coreclr/JIT/Methodical/Boxing/misc/tailjump.cs
#tailcall/coreclr/JIT/Methodical/Invoke/SEH/catchfinally_tail.cs
#tailcall/coreclr/JIT/Methodical/nonvirtualcall/tailcall.cs
#tailcall/coreclr/JIT/Methodical/tailcall/compat_i2_bool.il
#tailcall/coreclr/JIT/Methodical/tailcall/compat_i4_i1.il
#tailcall/coreclr/JIT/Methodical/tailcall/compat_i4_u.il
#tailcall/coreclr/JIT/Methodical/tailcall/compat_i_u2.il
#tailcall/coreclr/JIT/Methodical/tailcall/compat_r4_r8.il
#tailcall/coreclr/JIT/Methodical/tailcall/compat_r8_r4.il

TEST_TAILCALL_IL_SRC=\
	tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyWithPrefix.il \
	tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyTransparentLibraryWithPrefix.il \
	tailcall/coreclr/JIT/opt/Tailcall/TailcallVerifyVerifiableLibraryWithPrefix.il \
	tailcall/coreclr/JIT/Directed/coverage/importer/badtailcall.il \
	tailcall/coreclr/JIT/Directed/coverage/importer/Desktop/badtailcall.il \
	tailcall/coreclr/JIT/Directed/IL/mutualrecur-tailcall/MutualRecur-TailCall.il \
	tailcall/coreclr/JIT/Directed/IL/PInvokeTail/tailwinapi.il \
	tailcall/coreclr/JIT/Directed/IL/Tailcall/JitTailcall1.il \
	tailcall/coreclr/JIT/Directed/IL/Tailcall/Jittailcall2.il \
	tailcall/coreclr/JIT/Directed/pinvoke/tail.il \
	tailcall/coreclr/JIT/Directed/tailcall/tailcall.il \
	tailcall/coreclr/JIT/IL_Conformance/Old/Base/tailcall.il \
	tailcall/coreclr/JIT/Methodical/Boxing/boxunbox/tailcall.il \
	tailcall/coreclr/JIT/Methodical/Boxing/misc/tailjump.il \
	tailcall/coreclr/JIT/Methodical/Invoke/fptr/recurse_tail_call.il \
	tailcall/coreclr/JIT/Methodical/Invoke/fptr/recurse_tail_calli.il \
	tailcall/coreclr/JIT/Methodical/Invoke/SEH/catchfault_tail.il \
	tailcall/coreclr/JIT/Methodical/Invoke/SEH/catchfinally_tail.il \
	tailcall/coreclr/JIT/Methodical/nonvirtualcall/tailcall.il \
	tailcall/coreclr/JIT/Methodical/tailcall/compat_obj.il \
	tailcall/coreclr/JIT/Methodical/tailcall/compat_v.il \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_array.il \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_array_nz.il \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_gc.il \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_inst.il \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_value.il \
	tailcall/coreclr/JIT/Methodical/tailcall/deep_virt.il \
	tailcall/coreclr/JIT/Methodical/tailcall/gcval.il \
	tailcall/coreclr/JIT/Methodical/tailcall/gcval_nested.il \
	tailcall/coreclr/JIT/Methodical/tailcall/gcval_sideeffect.il \
	tailcall/coreclr/JIT/Methodical/tailcall/pointer.il \
	tailcall/coreclr/JIT/Methodical/tailcall/pointer_i.il \
	tailcall/coreclr/JIT/Methodical/tailcall/recurse_ep.il \
	tailcall/coreclr/JIT/Methodical/tailcall/recurse_ep_void.il \
	tailcall/coreclr/JIT/Methodical/tailcall/reference_i.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_2a.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_2b.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_2c.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_3b.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_implicit.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_mutual_rec.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_switch.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_virt.il \
	tailcall/coreclr/JIT/Methodical/tailcall/test_void.il \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/delegateTail.il \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/hijacking.il \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/smallFrame.il \
	tailcall/coreclr/JIT/Methodical/tailcall_v4/tailcall_AV.il \
	tailcall/coreclr/JIT/opt/ETW/TailCallCases.il \
	tailcall/coreclr/JIT/Regression/VS-ia64-JIT/V1.2-M02/b102844/tailcallcalli.il

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
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par-dyn MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par-512k MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par-32m MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-regular-ms-conc-par-simple-par-dyn-clear-at-gc MONO_TESTS_STRESS=1 || ok=false; \
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
test-sgen-regular-ms-conc-par-simple-par-dyn: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc-par,minor=simple-par,dynamic-nursery" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple-par-512k: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=512k" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple-par-32m: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=32m" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)
test-sgen-regular-ms-conc-par-simple-par-dyn-clear-at-gc: $(TESTS_SGEN_REGULAR) $(TESTSAOT_SGEN_REGULAR) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=clear-at-gc --gc-params=major=marksweep-conc-par,minor=simple-par,dynamic-nursery" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_REGULAR)

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
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par-dyn MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par-512k MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par-32m MONO_TESTS_STRESS=1 || ok=false; \
	$(MAKE) test-sgen-toggleref-ms-conc-par-simple-par-dyn-clear-at-gc MONO_TESTS_STRESS=1 || ok=false; \
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
test-sgen-toggleref-ms-conc-par-simple-par-dyn: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par,dynamic-nursery" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple-par-512k: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par,nursery-size=512k" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple-par-32m: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug= --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par,nursery-size=32m" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)
test-sgen-toggleref-ms-conc-par-simple-par-dyn-clear-at-gc: $(TESTS_SGEN_TOGGLEREF) $(TESTSAOT_SGEN_TOGGLEREF) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=clear-at-gc --gc-params=toggleref-test,major=marksweep-conc-par,minor=simple-par,dynamic-nursery" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_TOGGLEREF)

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
	$(MAKE) test-sgen-bridge-ms-conc-par-simple-par-dyn-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
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
test-sgen-bridge-ms-conc-par-simple-par-dyn-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,dynamic-nursery,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-par-simple-par-512k-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=512k,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)
test-sgen-bridge-ms-conc-par-simple-par-32m-tarjan-bridge: $(TESTS_SGEN_BRIDGE) $(TESTSAOT_SGEN_BRIDGE) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,nursery-size=32m,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE)

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
	$(MAKE) test-sgen-bridge2-ms-conc-par-simple-par-dyn-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
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
test-sgen-bridge2-ms-conc-par-simple-par-dyn-tarjan-bridge: $(TESTS_SGEN_BRIDGE2) $(TESTSAOT_SGEN_BRIDGE2) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=2Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,dynamic-nursery,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE2)
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
	$(MAKE) test-sgen-bridge3-ms-conc-par-simple-par-dyn-tarjan-bridge MONO_TESTS_STRESS=1 || ok=false; \
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
test-sgen-bridge3-ms-conc-par-simple-par-dyn-tarjan-bridge: $(TESTS_SGEN_BRIDGE3) $(TESTSAOT_SGEN_BRIDGE3) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --runtime-args "--gc=sgen --gc-debug=bridge=3Bridge --gc-params=major=marksweep-conc-par,minor=simple-par,dynamic-nursery,bridge-implementation=tarjan" --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_SGEN_BRIDGE3)
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

if HOST_WIN32
AOT_CONFIGURATIONS=	\
	"|regular"
else
AOT_CONFIGURATIONS=	\
	"|regular"	\
	"--gc=boehm|boehm"
endif

#LLVM crashes, got to fix it first.
#	"--llvm|llvm"	\
#	"--llvm --gc=boehm|llvm+boehm"

AOT_TEMP_DIR=$(top_builddir)/mono/tests/aot-tmp

test-aot:
	@failed=0; \
	passed=0; \
	failed_tests="";\
	profile=$(DEFAULT_PROFILE);	\
	rm -rf $(AOT_TEMP_DIR); \
	mkdir $(AOT_TEMP_DIR); \
	rm -f test-aot-$${name}.stdout test-aot-$${name}.stderr; \
	for assembly in $(mcs_topdir)/class/lib/$$profile/*.dll	; do	\
		asm_name=`basename $$assembly`;	\
		echo "... $$asm_name";	\
		for conf in $(AOT_CONFIGURATIONS); do	\
			name=`echo $$conf | cut -d\| -f 2`;	\
			params=`echo $$conf | cut -d\| -f 1`;	\
			test_name="$${asm_name}|$${name}";	\
			echo "  $$test_name";	\
			if MONO_PATH=$(mcs_topdir)/class/lib/$$profile $(JITTEST_PROG_RUN) $$params --aot=temp-path=$(AOT_TEMP_DIR),outfile=$(AOT_TEMP_DIR)/$$asm_name $$assembly >> test-aot-$${name}.stdout 2>> test-aot-$${name}.stderr;	\
			then \
				passed=`expr $${passed} + 1`; \
			else \
				failed=`expr $${failed} + 1`; \
				failed_tests="$${failed_tests} $$test_name"; \
			fi \
		done;	\
	done;	\
	rm -rf $(AOT_TEMP_DIR); \
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
	marshalbool.exe marshal9.exe marshal5.exe marshal.exe handleref.exe cominterop.exe bug-Xamarin-5278.exe \
	bug-58782-plain-throw.exe bug-58782-capture-and-throw.exe install_eh_callback.exe ccw-class-iface.exe: libtest.la

EXTRA_DIST += event-il.il

event-get.2.exe$(PLATFORM_AOT_SUFFIX): event-il.exe$(PLATFORM_AOT_SUFFIX)
event-get.2.exe: event-il.exe

EXTRA_DIST += module-cctor.il

module-cctor-loader.2.exe$(PLATFORM_AOT_SUFFIX): module-cctor.exe$(PLATFORM_AOT_SUFFIX)
module-cctor-loader.2.exe: module-cctor.exe

EXTRA_DIST += TestingReferenceAssembly.cs TestingReferenceReferenceAssembly.cs

reference-loader.exe$(PLATFORM_AOT_SUFFIX): TestingReferenceAssembly.dll$(PLATFORM_AOT_SUFFIX) TestingReferenceReferenceAssembly.dll$(PLATFORM_AOT_SUFFIX)
reference-loader.exe: TestingReferenceAssembly.dll TestingReferenceReferenceAssembly.dll

assemblyresolve_asm.dll$(PLATFORM_AOT_SUFFIX): assemblyresolve_asm.dll assemblyresolve_deps/Test.dll$(PLATFORM_AOT_SUFFIX)
	MONO_PATH="assemblyresolve_deps:$(CLASS)" $(top_builddir)/runtime/mono-wrapper $(TEST_AOT_BUILD_FLAGS) assemblyresolve_asm.dll
assemblyresolve_deps/Test.dll$(PLATFORM_AOT_SUFFIX): assemblyresolve_deps/Test.dll assemblyresolve_deps/TestBase.dll$(PLATFORM_AOT_SUFFIX)

EXTRA_DIST += assemblyresolve_TestBase.cs assemblyresolve_Test.cs assemblyresolve_asm.cs 
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

EXTRA_DIST += assemblyresolve_event5_label.cs assemblyresolve_event5_helper.cs
assemblyresolve_deps/assemblyresolve_event5_label.dll: assemblyresolve_event5_label.cs assemblyresolve_deps 
	$(MCS) -target:library -out:assemblyresolve_deps/assemblyresolve_event5_label.dll $(srcdir)/assemblyresolve_event5_label.cs
assemblyresolve_event5_helper.dll: assemblyresolve_event5_helper.cs assemblyresolve_deps/assemblyresolve_event5_label.dll
	$(MCS) -target:library -out:assemblyresolve_event5_helper.dll -r:assemblyresolve_deps/assemblyresolve_event5_label.dll $(srcdir)/assemblyresolve_event5_helper.cs
assemblyresolve_event5.exe: assemblyresolve_event5_helper.dll

assemblyresolve_event6.exe$(PLATFORM_AOT_SUFFIX): assemblyresolve_asm.dll$(PLATFORM_AOT_SUFFIX) assemblyresolve_deps/Test.dll$(PLATFORM_AOT_SUFFIX) assemblyresolve_deps/TestBase.dll$(PLATFORM_AOT_SUFFIX)
assemblyresolve_event6.exe: assemblyresolve_asm.dll assemblyresolve_deps/Test.dll assemblyresolve_deps/TestBase.dll

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

EXTRA_DIST += appdomain-marshalbyref-assemblyload-MidAssembly.cs appdomain-marshalbyref-assemblyload-LeafAssembly.cs

LeafAssembly.dll: appdomain-marshalbyref-assemblyload-LeafAssembly.cs
	mkdir -p appdomain-marshalbyref-assemblyload1
	$(MCS) -target:library -out:$@ $<

appdomain-marshalbyref-assemblyload2/LeafAssembly.dll: appdomain-marshalbyref-assemblyload-LeafAssembly.cs
	mkdir -p appdomain-marshalbyref-assemblyload2
	$(MCS) -target:library -out:$@ $< -define:UNDEFINE_OTHER_METHOD

MidAssembly.dll: appdomain-marshalbyref-assemblyload-MidAssembly.cs LeafAssembly.dll
	mkdir -p appdomain-marshalbyref-assemblyload1
	$(MCS) -target:library -out:$@ $< -r:LeafAssembly.dll

appdomain-marshalbyref-assemblyload.exe: appdomain-marshalbyref-assemblyload.cs MidAssembly.dll LeafAssembly.dll appdomain-marshalbyref-assemblyload2/LeafAssembly.dll
	$(MCS) -out:$@ $< -r:MidAssembly.dll -r:LeafAssembly.dll

assembly-load-dir1/Lib.dll: assembly-load-dir1/Lib.cs
	$(MKDIR_P) $(dir $@)
	$(MCS) -target:library -out:$@ $<
assembly-load-dir2/Lib.dll: assembly-load-dir2/Lib.cs
	$(MKDIR_P) $(dir $@)
	$(MCS) -target:library -out:$@ $<

assembly-load-dir1/LibStrongName.dll: assembly-load-dir1/LibStrongName.cs testing_gac/testkey.snk
	$(MKDIR_P) $(dir $@)
	$(MCS) -target:library -out:$@ $< -keyfile:$(dir $<)../testing_gac/testkey.snk

assembly-load-dir2/LibStrongName.dll: assembly-load-dir2/LibStrongName.cs testing_gac/testkey.snk
	$(MKDIR_P) $(dir $@)
	$(MCS) -target:library -out:$@ $< -keyfile:$(dir $<)../testing_gac/testkey.snk

EXTRA_DIST += assembly-load-dir1/Lib.cs assembly-load-dir2/Lib.cs \
	assembly-load-dir1/LibStrongName.cs assembly-load-dir2/LibStrongName.cs

assembly-load-bytes.exe: assembly-load-dir1/Lib.dll assembly-load-dir2/Lib.dll

assembly-loadfrom.exe$(PLATFORM_AOT_SUFFIX): assembly-load-dir1/Lib.dll$(PLATFORM_AOT_SUFFIX) assembly-load-dir2/Lib.dll$(PLATFORM_AOT_SUFFIX)
assembly-loadfrom.exe: assembly-load-dir1/Lib.dll assembly-load-dir2/Lib.dll

assembly-loadfile.exe$(PLATFORM_AOT_SUFFIX): assembly-load-dir1/Lib.dll$(PLATFORM_AOT_SUFFIX) assembly-load-dir2/Lib.dll$(PLATFORM_AOT_SUFFIX)
assembly-loadfile.exe: assembly-load-dir1/Lib.dll assembly-load-dir2/Lib.dll

assembly-loadfrom-bindingredirect.exe$(PLATFORM_AOT_SUFFIX): assembly-load-dir1/LibStrongName.dll$(PLATFORM_AOT_SUFFIX) assembly-load-dir2/LibStrongName.dll$(PLATFORM_AOT_SUFFIX) assembly-loadfrom-bindingredirect.exe.config
assembly-loadfrom-bindingredirect.exe: assembly-load-dir1/LibStrongName.dll assembly-load-dir2/LibStrongName.dll assembly-loadfrom-bindingredirect.exe.config

assembly-loadfile-bindingredirect.exe$(PLATFORM_AOT_SUFFIX): assembly-load-dir1/LibStrongName.dll$(PLATFORM_AOT_SUFFIX) assembly-load-dir2/LibStrongName.dll$(PLATFORM_AOT_SUFFIX) assembly-loadfile-bindingredirect.exe.config
assembly-loadfile-bindingredirect.exe: assembly-load-dir1/LibStrongName.dll assembly-load-dir2/LibStrongName.dll assembly-loadfile-bindingredirect.exe.config

assembly-load-bytes-bindingredirect.exe: assembly-load-dir1/LibStrongName.dll assembly-load-dir2/LibStrongName.dll assembly-load-bytes-bindingredirect.exe.config

assembly-refonly-load-bytes-bindingredirect.exe: assembly-load-dir1/LibStrongName.dll assembly-load-dir2/LibStrongName.dll assembly-refonly-load-bytes-bindingredirect.exe.config


EXTRA_DIST += assembly-loadfrom-bindingredirect.exe.config assembly-loadfile-bindingredirect.exe.config assembly-load-bytes-bindingredirect.exe.config
EXTRA_DIST += assembly-refonly-load-bytes-bindingredirect.exe.config

assembly-load-dir1/LibSimpleName.dll: assembly-load-dir1/LibSimpleName.cs
	$(MKDIR_P) $(dir $@)
	$(MCS) -target:library -out:$@ $<

# NOTE: unlike assembly-load-dir1/LibSimpleName.dll, the output name here is in all lower case!
assembly-load-dir2/libsimplename.dll: assembly-load-dir2/LibSimpleName.cs
	$(MKDIR_P) $(dir $@)
	$(MCS) -target:library -out:$@ $<

assembly-dep-simplename.dll: assembly-dep-simplename.cs assembly-load-dir1/LibSimpleName.dll
	$(MCS) -target:library -out:$@ $< -r:assembly-load-dir1/LibSimpleName.dll

# NOTE: while we compile assembly-dep-simplename.dll using assembly-load-dir1, we AOT it using assembly-load-dir2
#  (So that the AOT image has the right GUID of the referenced assembly baked in)
assembly-dep-simplename.dll$(PLATFORM_AOT_SUFFIX): assembly-dep-simplename.dll assembly-load-dir2/libsimplename.dll$(PLATFORM_AOT_SUFFIX)
	MONO_PATH=$(CLASS):assembly-load-dir2 $(top_builddir)/runtime/mono-wrapper $(TEST_AOT_BUILD_FLAGS) --aot-path=assembly-load-dir2 assembly-dep-simplename.dll

EXTRA_DIST += assembly-load-dir1/LibSimpleName.cs assembly-load-dir2/LibSimpleName.cs \
	assembly-dep-simplename.cs

assembly-loadfrom-simplename.exe: assembly-dep-simplename.dll assembly-load-dir2/libsimplename.dll

assembly-loadfrom-simplename.exe$(PLATFORM_AOT_SUFFIX): assembly-load-dir2/libsimplename.dll$(PLATFORM_AOT_SUFFIX) assembly-dep-simplename.dll$(PLATFORM_AOT_SUFFIX)

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

test-generic-sharing:
	@if test x$(M) != x0; then $(MAKE) test-generic-sharing-managed; else $(MAKE) test-generic-sharing-normal; fi

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


EXTRA_DIST += threadpool-in-processexit.cs threadpool-in-processexit.exe.stdout.expected
test-process-exit:
	@$(MCS) $(srcdir)/threadpool-in-processexit.cs -out:threadpool-in-processexit.exe
	@echo "Testing threadpool-in-processexit.exe..."
	@$(RUNTIME) threadpool-in-processexit.exe > threadpool-in-processexit.exe.stdout
	@diff -w threadpool-in-processexit.exe.stdout $(srcdir)/threadpool-in-processexit.exe.stdout.expected

# tests that expect a 1 exit code
TESTS_UNHANDLED_EXCEPTION_1_SRC =	\
	unhandled-exception-1.cs	\
	unhandled-exception-9.cs

# tests that expect a 255 exit code
TESTS_UNHANDLED_EXCEPTION_255_SRC =	\
	unhandled-exception-2.cs	\
	unhandled-exception-3.cs	\
	unhandled-exception-4.cs	\
	unhandled-exception-5.cs	\
	unhandled-exception-6.cs	\
	unhandled-exception-7.cs	\
	unhandled-exception-8.cs	\
	threadpool-exceptions2.cs	\
	threadpool-exceptions5.cs

EXTRA_DIST += $(TESTS_UNHANDLED_EXCEPTION_1_SRC) $(TESTS_UNHANDLED_EXCEPTION_255_SRC)

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

.PHONY: test-gac-loading test-eglib-remap

runtest-gac-loading: test-runner.exe
	$(MAKE) -C testing_gac run-test

compile-gac-loading:
	$(MAKE) -C testing_gac test

runtest-assembly-load-reference: test-runner.exe
	$(MAKE) -C assembly-load-reference run-test

compile-assembly-load-reference:
	$(MAKE) -C assembly-load-reference test


TESTS_STRESS_PROCESS_SRC=	\
		process-stress-1.cs	\
		process-stress-2.cs	\
		process-stress-3.cs	\
		process-leak.cs

EXTRA_DIST += $(TESTS_STRESS_PROCESS_SRC)

TESTS_STRESS_PROCESS=$(filter-out $(DISABLED_TESTS),$(TESTS_STRESS_PROCESS_SRC:.cs=.exe))

if FULL_AOT_TESTS
TESTSAOT_STRESS_PROCESS=$(TESTS_STRESS_PROCESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
TESTSAOT_STRESS_PROCESS=$(TESTS_STRESS_PROCESS:.exe=.exe$(PLATFORM_AOT_SUFFIX))
endif

test-process-stress: $(TESTS_STRESS_PROCESS) $(TESTSAOT_STRESS_PROCESS) test-runner.exe
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ --disabled "$(DISABLED_TESTS)" --timeout 900 $(TESTS_STRESS_PROCESS)

coreclr-gcstress:
	$(MAKE) -C $(mono_build_root)/acceptance-tests coreclr-gcstress

# Tests for the Mono lldb plugin
EXTRA_DIST += test_lldb.py test-lldb.cs
test-lldb: test-lldb.exe
	$(PYTHON) test_lldb.py $(JITTEST_PROG)

if !ENABLE_MSVC_ONLY

noinst_LTLIBRARIES = libtest.la

AM_CPPFLAGS = $(GLIB_CFLAGS)

if HOST_WIN32
# gcc-3.4.4 emits incorrect code when making indirect calls to stdcall functions using a tail call
# This shows up when compiling mono_test_marshal_delegate ()
libtest_la_CFLAGS=-fno-optimize-sibling-calls
# the exported names created by gcc for stdcall functions are missing the leading _, so MS.NET
# can't find them. So we use --kill-at to remove the @ suffix as well.
libtest_la_LDFLAGS = -no-undefined -rpath `pwd` -Wl,--kill-at
else
libtest_la_LDFLAGS = -no-undefined -rpath `pwd`
endif
libtest_la_SOURCES = libtest.c
libtest_la_LIBADD = $(top_builddir)/mono/eglib/libeglib.la

endif # !ENABLE_MSVC_ONLY

INTERNALSVISIBLETO_TEST_SRC = \
	internalsvisibleto-runtimetest.cs \
	internalsvisibleto-compilertest.cs

INTERNALSVISIBLETO_TEST_LIB_SRC = \
	internalsvisibleto-library.cs

INTERNALSVISIBLETO_TEST_LIBS = \
	internalsvisibleto-correctcase.dll \
	internalsvisibleto-wrongcase.dll \
	internalsvisibleto-correctcase-2.dll \
	internalsvisibleto-wrongcase-2.dll \
	internalsvisibleto-correctcase-sign2048.dll \
	internalsvisibleto-wrongcase-sign2048.dll \
	internalsvisibleto-correctcase-2-sign2048.dll \
	internalsvisibleto-wrongcase-2-sign2048.dll


INTERNALSVISIBLETO_TEST = \
	$(INTERNALSVISIBLETO_TEST_SRC:.cs=.exe) \
	$(INTERNALSVISIBLETO_TEST_SRC:.cs=-sign2048.exe)

if FULL_AOT_TESTS
INTERNALSVISIBLETO_TESTAOT = \
	$(INTERNALSVISIBLETO_TEST:.exe=.exe$(PLATFORM_AOT_SUFFIX))
INTERNALSVISIBLETO_TESTAOT_LIBS = \
	$(INTERNALSVISIBLETO_TEST_LIBS:.dll=.dll$(PLATFORM_AOT_SUFFIX))
endif

if HYBRID_AOT_TESTS
INTERNALSVISIBLETO_TESTAOT = \
	$(INTERNALSVISIBLETO_TEST:.exe=.exe$(PLATFORM_AOT_SUFFIX))
INTERNALSVISIBLETO_TESTAOT_LIBS = \
	$(INTERNALSVISIBLETO_TEST_LIBS:.dll=.dll$(PLATFORM_AOT_SUFFIX))
endif

EXTRA_DIST += $(INTERNALSVISIBLETO_TEST_SRC) $(INTERNALSVISIBLETO_TEST_LIB_SRC)

test-internalsvisibleto: test-runner.exe $(INTERNALSVISIBLETO_TEST) $(INTERNALSVISIBLETO_TESTAOT) $(INTERNALSVISIBLETO_TESTAOT_LIBS)
	$(TOOLS_RUNTIME) $(TEST_RUNNER) --testsuite-name $@ $(INTERNALSVISIBLETO_TEST)

internalsvisibleto-correctcase.dll internalsvisibleto-wrongcase.dll internalsvisibleto-runtimetest.exe: internalsvisibleto-runtimetest.cs internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-correctcase.dll -target:library -d:CORRECT_CASE -d:PERMISSIVE internalsvisibleto-library.cs	
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-wrongcase.dll -target:library -d:WRONG_CASE -d:PERMISSIVE internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-runtimetest.exe -warn:0 -r:internalsvisibleto-correctcase.dll -r:internalsvisibleto-wrongcase.dll internalsvisibleto-runtimetest.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-correctcase.dll -target:library -d:CORRECT_CASE internalsvisibleto-library.cs	
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-wrongcase.dll -target:library -d:WRONG_CASE internalsvisibleto-library.cs

internalsvisibleto-correctcase-2.dll internalsvisibleto-wrongcase-2.dll  internalsvisibleto-compilertest.exe: internalsvisibleto-compilertest.cs internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-correctcase-2.dll -target:library -d:CORRECT_CASE internalsvisibleto-library.cs	
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-wrongcase-2.dll -target:library -d:WRONG_CASE internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-compilertest.exe -warn:0 -r:internalsvisibleto-correctcase-2.dll -r:internalsvisibleto-wrongcase-2.dll internalsvisibleto-compilertest.cs

internalsvisibleto-correctcase-sign2048.dll internalsvisibleto-wrongcase-sign2048.dll internalsvisibleto-runtimetest-sign2048.exe: internalsvisibleto-runtimetest.cs internalsvisibleto-library.cs internalsvisibleto-2048.snk
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-correctcase-sign2048.dll -target:library -d:CORRECT_CASE -d:PERMISSIVE -d:SIGN2048 internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-wrongcase-sign2048.dll -target:library -d:WRONG_CASE -d:PERMISSIVE -d:SIGN2048 internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-runtimetest-sign2048.exe -warn:0 -r:internalsvisibleto-correctcase-sign2048.dll -r:internalsvisibleto-wrongcase-sign2048.dll -d:SIGN2048 internalsvisibleto-runtimetest.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-correctcase-sign2048.dll -target:library -d:CORRECT_CASE -d:SIGN2048 internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-wrongcase-sign2048.dll -target:library -d:WRONG_CASE -d:SIGN2048 internalsvisibleto-library.cs

internalsvisibleto-correctcase-2-sign2048.dll internalsvisibleto-wrongcase-2-sign2048.dll internalsvisibleto-compilertest-sign2048.exe: internalsvisibleto-compilertest.cs internalsvisibleto-library.cs internalsvisibleto-2048.snk
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-correctcase-2-sign2048.dll -target:library -d:CORRECT_CASE -d:SIGN2048 internalsvisibleto-library.cs	
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-wrongcase-2-sign2048.dll -target:library -d:WRONG_CASE -d:SIGN2048 internalsvisibleto-library.cs
	$(Q) $(MCS_NO_UNSAFE) -out:internalsvisibleto-compilertest-sign2048.exe -warn:0 -r:internalsvisibleto-correctcase-2-sign2048.dll -r:internalsvisibleto-wrongcase-2-sign2048.dll -d:SIGN2048 internalsvisibleto-compilertest.cs

EXTRA_DIST += internalsvisibleto-runtimetest.cs internalsvisibleto-compilertest.cs internalsvisibleto-library.cs internalsvisibleto-2048.snk

EXTRA_DIST += weakattribute.cs

EXTRA_DIST += null-blob-null-blob-assm.il null-blob-ref.il null-blob-tgt.cs

null-blob-tgt.dll: null-blob-tgt.cs
	$(MCS) -target:library -out:$@ $<

null-blob-main.exe: null-blob-main.cs null-blob-tgt.dll null-blob-ref.dll null-blob-null-blob-assm.dll
	$(MCS) -target:exe -out:$@ $<

null-blob-main.exe$(PLATFORM_AOT_SUFFIX): null-blob-tgt.dll$(PLATFORM_AOT_SUFFIX) null-blob-ref.dll$(PLATFORM_AOT_SUFFIX) null-blob-null-blob-assm.dll$(PLATFORM_AOT_SUFFIX)

# Contains copies of types which don't exist in the desktop profile so tests can use them
Mono.Runtime.Testing.dll: weakattribute.cs
	$(MCS) -target:library -out:$@ $<

weak-fields.exe: weak-fields.cs Mono.Runtime.Testing.dll
	$(MCS) -r:Mono.Runtime.Testing.dll -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -r:TestDriver.dll $(TEST_DRIVER_HARD_KILL_FEATURE) -out:$@ $<

CLEANFILES = $(TESTS_REGULAR) $(TESTS_STRESS) $(TESTSAOT_CS) $(TESTSAOT_IL) $(TESTSAOT_BENCH) $(TESTSAOT_STRESS) *.dll *.stdout *.aotlog *.exe stest.dat LeafAssembly.dll MidAssembly.dll appdomain-marshalbyref-assemblyload2/*.dll 
CLEANFILES += $(TESTS_TAILCALL_COMPILE) $(TESTSAOT_TAILCALL)
CLEANFILES += $(BUILT_SOURCES)
