SUBDIRS = cas assemblyresolve

check-local: test

aotcheck: testaot gshared-aot

TEST_PROG = ../interpreter/mint

JITTEST_PROG = $(if $(VALGRIND), valgrind $(VALGRIND_ARGS),) $(if $(SGEN),$(top_builddir)/mono/mini/mono-sgen,$(top_builddir)/mono/mini/mono)

TEST_PROG_RUN = MONO_SHARED_DIR=$(mono_build_root)/runtime MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(TEST_PROG)
JITTEST_PROG_RUN = MONO_SHARED_DIR=$(mono_build_root)/runtime MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(JITTEST_PROG)

RUNTIME_ARGS=--config tests-config --optimize=all --debug

with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_4_5
with_mono_path_moonlight = MONO_PATH=$(mcs_topdir)/class/lib/moonlight_raw

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper
RUNTIME_MOONLIGHT = $(with_mono_path_moonlight) $(top_builddir)/runtime/mono-wrapper

MKBUNDLE = \
	PKG_CONFIG_PATH=$(top_builddir):$(PKG_CONFIG_PATH) \
	$(RUNTIME) $(mcs_topdir)/class/lib/net_4_5/mkbundle.exe

MCS = $(RUNTIME) $(mcs_topdir)/class/lib/build/mcs.exe -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219 -debug
ILASM = $(RUNTIME) $(mcs_topdir)/class/lib/net_4_5/ilasm.exe

BENCHSRC=fib.cs random.cs nested-loops.cs ackermann.cs tight-loop.cs sieve.cs

STRESS_TESTS_SRC=	\
	abort-stress-1.cs	\
	abort-stress-2.cs	\
	abort-stress-3.cs	\
	domain-stress.cs	\
	gchandle-stress.cs	\
	monitor-stress.cs	\
	thread-stress.cs	\
	gc-stress.cs		\
	exit-stress.cs		\
	process-stress.cs	\
	assembly-load-stress.cs

# Disabled until ?mcs is fixed
#	bug-331958.cs
BASE_TEST_CS_SRC=		\
	array-init.cs		\
	arraylist.cs		\
	assemblyresolve_event.cs	\
	assemblyresolve_event3.cs	\
	checked.cs		\
	char-isnumber.cs	\
	create-instance.cs	\
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
	typeload-unaligned.cs	\
	struct.cs		\
	valuetype-gettype.cs	\
	typeof-ptr.cs		\
	static-constructor.cs	\
	pinvoke.cs		\
	pinvoke2.cs		\
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
	appdomain.cs        \
	appdomain1.cs		\
	appdomain2.cs		\
	appdomain-client.cs	\
	appdomain-unload.cs \
	appdomain-async-invoke.cs	\
	loader.cs       \
	pointer.cs		\
	hashcode.cs		\
	delegate1.cs		\
	delegate2.cs		\
	delegate3.cs		\
	delegate5.cs		\
	delegate6.cs		\
	delegate7.cs		\
	delegate8.cs		\
	delegate9.cs		\
	remoting1.cs		\
	remoting2.cs		\
	remoting3.cs		\
	remoting4.cs		\
	remoting5.cs		\
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
	marshal-valuetypes.cs	\
	test-byval-in-struct.cs	\
	thread.cs		\
	thread5.cs		\
	thread6.cs		\
	thread-static.cs	\
	thread-static-init.cs	\
	context-static.cs	\
	float-pop.cs		\
	interfacecast.cs	\
	array3.cs		\
	classinit.cs		\
	classinit2.cs		\
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
	threadpool-exceptions7.cs \
	base-definition.cs	\
	bug-27420.cs		\
	bug-47295.cs		\
	bug-46781.cs		\
	bug-48015.cs		\
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
	cross-domain.cs		\
	appdomain-exit.cs	\
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
	cominterop.cs		\
	exists.cs		\
	handleref.cs	\
	transparentproxy.cs \
	dbnull-missing.cs	\
	test-type-ctor.cs 	\
	soft-float-tests.cs	\
	thread-exit.cs		\
	finalize-parent.cs	\
	assemblyresolve_event2.2.cs	\
	interlocked-2.2.cs	\
	pinvoke-2.2.cs 		\
	bug-78431.2.cs 		\
	bug-79684.2.cs 		\
	catch-generics.2.cs	\
	event-get.2.cs		\
	safehandle.2.cs		\
	stackframes-async.2.cs		\
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
	generic-exceptions.2.cs	\
	generic-virtual2.2.cs	\
	generic-valuetype-interface.2.cs	\
	generic-getgenericarguments.2.cs	\
	generic-type-builder.2.cs	\
	generic-synchronized.2.cs	\
	generic-delegate-ctor.2.cs	\
	generic-array-iface-set.2.cs	\
	generic-typedef.2.cs	\
	generic-marshalbyref.2.cs	\
	generic-xdomain.2.cs	\
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
	dynamic-method-access.2.cs	\
	dynamic-method-finalize.2.cs	\
	bug-82194.2.cs	\
	anonarray.2.cs	\
	ienumerator-interfaces.2.cs	\
	array-enumerator-ifaces.2.cs	\
	generic_type_definition_encoding.2.cs \
	generic_type_definition.2.cs	\
	bug-333798.2.cs		\
	bug-333798-tb.2.cs		\
	bug-335131.2.cs		\
	bug-322722_patch_bx.2.cs		\
	bug-348522.2.cs		\
	bug-340662_bug.cs	\
	bug-322722_dyn_method_throw.2.cs	\
	bug-389886-2.cs	\
	bug-325283.2.cs	\
	thunks.cs \
	winx64structs.cs \
	bug-349190.2.cs	\
	nullable_boxing.2.cs	\
	valuetype-equals.cs	\
	custom-modifiers.2.cs	\
	bug-382986.cs	\
	test-inline-call-stack.cs	\
	bug-324535.cs	\
	modules.cs	\
	bug-81673.cs	\
	bug-81691.cs	\
	bug-80307.cs	\
	bug-415577.cs	\
	filter-stack.cs	\
	vararg2.cs	\
	bug-389886-sre-generic-interface-instances.cs	\
	bug-461867.cs	\
	bug-461941.cs	\
	bug-461261.cs	\
	bug-400716.cs	\
	bug-462592.cs	\
	bug-459094.cs	\
	generic-unloading.2.cs	\
	generic-unloading-sub.2.cs	\
	bug-467456.cs	\
	appdomain-unload-callback.cs	\
	bug-508538.cs	\
	bug-472692.2.cs		\
	gchandles.cs	\
	interlocked-3.cs	\
	interlocked-4.2.cs	\
	finalizer-wait.cs	\
	critical-finalizers.cs	\
	appdomain-thread-abort.cs \
	xdomain-threads.cs	\
	w32message.cs	\
	bug-544446.cs	\
	gc-altstack.cs	\
	large-gc-bitmap.cs	\
	bug-561239.cs	\
	bug-562150.cs	\
	bug-575941.cs	\
	bug-599469.cs	\
	bug-389886-3.cs \
	monitor.cs	\
	dynamic-method-resurrection.cs	\
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
	appdomain-unload-doesnot-raise-pending-events.cs	\
	bug-6148.cs

TEST_CS_SRC_DIST=	\
	$(BASE_TEST_CS_SRC)	\
	async-exc-compilation.cs \
	filter-stack.cs	\
	finally_guard.cs

TEST_CS_SRC_GEN = \
	runtime-invoke.gen.cs \
	imt_big_iface_test.cs

if AMD64
TEST_CS_SRC = $(BASE_TEST_CS_SRC) $(TEST_CS_SRC_GEN) async-exc-compilation.cs finally_guard.cs
# #651684
PLATFORM_DISABLED_TESTS = finally_guard.exe
else
if X86
TEST_CS_SRC = $(BASE_TEST_CS_SRC) $(TEST_CS_SRC_GEN) async-exc-compilation.cs finally_guard.cs
else
TEST_CS_SRC = $(BASE_TEST_CS_SRC) $(TEST_CS_SRC_GEN)
endif
endif

if IA64
TEST_CS_SRC+=async-exc-compilation.cs filter-stack.cs
# bug #319249
PLATFORM_DISABLED_TESTS=exception17.exe
PLATFORM_DISABLED_TESTS+=winx64structs.exe
endif

if X86

if HOST_WIN32
PLATFORM_DISABLED_TESTS=async-exc-compilation.exe finally_guard.exe
endif

endif

if POWERPC
# bug #71274
PLATFORM_DISABLED_TESTS=finalizer-abort.exe finalizer-exception.exe finalizer-exit.exe
endif

if ARM
PLATFORM_DISABLED_TESTS=filter-stack.exe
endif

if MIPS
# monitor.exe is racy
PLATFORM_DISABLED_TESTS=filter-stack.exe monitor.exe
endif

if S390x
PLATFORM_DISABLED_TESTS=dynamic-method-resurrection.exe
#PLATFORM_DISABLED_TESTS=dynamic-method-resurrection.exe exception17.exe
endif

# The two finalizer tests only work under sgen
# gc-altstack.exe fails under boehm because it has no support for altstack
# bug-459094.exe creates an extremely deep directory tree
# delegate-invoke.exe depends on 929c6bc9b6d76a273f251e6f5dfacac36e9c38bd which was
# reverted.
# generic_type_definition.2.exe depends on COMPILER_ACCESS which is no longer supported.
DISABLED_TESTS=			\
	delegate-async-exception.exe	\
	bug-348522.2.exe	\
	finalizer-wait.exe	\
	critical-finalizers.exe	\
	gc-altstack.exe \
	bug-459094.exe \
	delegate-invoke.exe \
	generic_type_definition.2.exe \
	$(PLATFORM_DISABLED_TESTS)

DISABLED_TESTS_WRENCH=	\
	$(DISABLED_TESTS)	\
	$(PLATFORM_DISABLED_TESTS_WRENCH)	\
	main-returns-background-resetabort.exe \
	main-returns-background-abort-resetabort.exe	\
	main-returns-background.exe	\
	thread6.exe	\
	assemblyresolve_event3.exe \
	delegate2.exe	\
	finally_guard.exe  	\
	finalizer-abort.exe	\
	finalizer-exit.exe	\
	finalizer-thread.exe \
	appdomain-async-invoke.exe

AOT_DISABLED_TESTS=constraints-load.exe

# These only compile with MS CSC
TEST_CSC_SRC=			\
	vararg.cs

TEST_IL_SRC=			\
	field-access.il		\
	method-access.il	\
	ldftn-access.il		\
	cpblkTest.il		\
	vbinterface.il		\
	calliTest.il		\
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
	constraints-load.il	\
	array_load_exception.il \
	bug-481403.il	\
	interface-with-static-method.il	\
	bug-515884.il	\
	bug-633291.il	\
	delegate-with-null-target.il	\
	bug-318677.il	\
	gsharing-valuetype-layout.il	\
	invalid_generic_instantiation.il


# pre-requisite test sources: files that are not test themselves
# but that need to be compiled
PREREQ_IL_SRC=event-il.il module-cctor.il
PREREQ_CS_SRC=
PREREQ_IL_DLL_SRC=event-il.il module-cctor.il
PREREQ_CS_DLL_SRC=

PREREQSI_IL=$(PREREQ_IL_SRC:.il=.exe)
PREREQSI_CS=$(PREREQ_CS_SRC:.cs=.exe)
TESTSI_CS=$(TEST_CS_SRC:.cs=.exe)
TESTSI_IL=$(TEST_IL_SRC:.il=.exe)
TESTBS=$(BENCHSRC:.cs=.exe)
STRESS_TESTS=$(STRESS_TESTS_SRC:.cs=.exe)

EXTRA_DIST=test-driver $(TEST_CS_SRC_DIST) $(TEST_IL_SRC) \
	$(BENCHSRC) $(STRESS_TESTS_SRC) stress-runner.pl $(PREREQ_IL_SRC) $(PREREQ_CS_SRC)

%.exe: %.il
	$(ILASM) -out:$@ $<

%.exe: %.cs TestDriver.dll
	$(MCS) -r:TestDriver.dll -out:$@ $<

# mkbundle works on ppc, but the pkg-config POC doesn't when run with make test
if POWERPC
test_platform:
else
# Can't use mkbundle on win32 since there is no static build there
# Can't run test-unhandled-exception on Windows because of all the debug popups...
if HOST_WIN32
test_platform:
else
# mkbundle uses the installed mono-2.pc so it won't work if there is no system mono
#test_platform:	testbundle test-iomap-regression
test_platform:	test-iomap-regression
endif
endif

if X86
if HOST_WIN32
else
test_platform: test-eglib-remap
endif
endif

# Target to precompile the test executables
tests: $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la $(PREREQSI_IL) $(PREREQSI_CS) $(GSHARED_TESTS)

#
# Test that no symbols are missed in eglib-remap.h
#
OK_G_SYMBOLS='g_list\|g_slist\|g_concat_dir_and_file'
test-eglib-remap:
	@echo "Testing eglib remap..."
	@if which nm > /dev/null; then if nm $(top_builddir)/mono/mini/mono | grep -v $(OK_G_SYMBOLS) | grep 't g_'; then exit 1; else exit 0; fi; fi

#
# Tests that the internals in mono/io-layer/messages.c are ok by triggering the 
# code that checks that the table is properly sorted
#
test-messages: w32message.exe
	> test_messages.zero
	$(with_mono_path) $(JITTEST_PROG_RUN) w32message.exe >& w32message.allout && cmp test_messages.zero w32message.allout

if MOONLIGHT
test_2_1 : test-coreclr-security
else
test_2_1 :
endif

if AMD64
test-sgen : sgen-tests
else
if X86
test-sgen : sgen-tests
else
if ARM
test-sgen : sgen-tests
else
if S390x
test-sgen : sgen-regular-tests
endif
endif
endif
endif

test: assemblyresolve/test/asm.dll testjit test-type-load test-generic-sharing test_platform test_2_1 test-messages
test-wrench: assemblyresolve/test/asm.dll testjit-wrench test-generic-sharing test-type-load test_platform test_2_1 test-process-exit test-sgen test-messages rm-empty-logs

# Remove empty .stdout and .stderr files for wrench
rm-empty-logs:
	@echo "Removing empty logs..."
	@find . '(' -name "*.stdout" -o -name "*.stderr" ')' -empty -exec rm {} \;

assemblyresolve/test/asm.dll:
	$(MAKE) -C assemblyresolve prereq

TestDriver.dll:
	$(MCS) -target:library -out:$@ $(srcdir)/../mini/TestDriver.cs

test_cs: $(TEST_PROG) $(TESTSI_CS) libtest.la
	@failed=0; \
	passed=0; \
	for i in $(TESTSI_CS); do	\
		if $(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".cs: $${passed} test(s) passed. $${failed} test(s) did not pass."; echo

test_il: $(TEST_PROG) $(TESTSI_IL) libtest.la
	@failed=0; \
	passed=0; \
	for i in $(TESTSI_IL); do	\
		if $(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".il: $${passed} test(s) passed. $${failed} test(s) did not pass."; echo

testb: $(TEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		$(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS);	\
	done

runtest-wrench: $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la $(PREREQSI_IL) $(PREREQSI_CS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTSI_CS) $(TESTBS) $(TESTSI_IL); do	\
		rm -f $${i}.so; \
		if [ x$(AOT) = x1 ]; then if echo $(AOT_DISABLED_TESTS) | grep -v -q $${i}; then $(with_mono_path) $(JITTEST_PROG_RUN) --aot --debug $${i} > $${i}.aotlog 2>&1 || exit 1; fi; fi; \
		if $(srcdir)/test-driver '$(with_mono_path) $(JITTEST_PROG_RUN)' $$i '$(DISABLED_TESTS_WRENCH)' $(RUNTIME_ARGS); \
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

runtest-managed-wrench: test-runner.exe $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la $(PREREQSI_IL) $(PREREQSI_CS)
	@$(RUNTIME) ./test-runner.exe -j a --disabled '$(DISABLED_TESTS_WRENCH)' $(TESTSI_CS) $(TESTBS) $(TESTSI_IL)

testjit-wrench:
	@if test x$(M) != x; then $(MAKE) runtest-managed-wrench; else $(MAKE) runtest-wrench; fi

runtest: $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la $(PREREQSI_IL) $(PREREQSI_CS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTSI_CS) $(TESTBS) $(TESTSI_IL); do	\
		rm -f $${i}.so; \
		if [ x$(AOT) = x1 ]; then if echo $(AOT_DISABLED_TESTS) | grep -v -q $${i}; then $(with_mono_path) $(JITTEST_PROG_RUN) --aot --debug $${i} > $${i}.aotlog 2>&1 || exit 1; fi; fi; \
		if $(srcdir)/test-driver '$(with_mono_path) $(JITTEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
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

runtest-managed: test-runner.exe $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la $(PREREQSI_IL) $(PREREQSI_CS)
	@$(RUNTIME) ./test-runner.exe -j a --disabled '$(DISABLED_TESTS)' $(TESTSI_CS) $(TESTBS) $(TESTSI_IL)

testjit:
	@if test x$(M) != x; then $(MAKE) runtest-managed; else $(MAKE) runtest; fi

testaot:
	@$(MAKE) AOT=1 runtest

testtrace:
	@$(MAKE) RUNTIME_ARGS="$${RUNTIME_ARGS} --trace" runtest

testinterp:
	@$(MAKE) JITTEST_PROG_RUN="$(TEST_PROG_RUN)" runtest

testjitspeed: $(JITTEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		echo $$i;	\
		time $(JITTEST_PROG) $$i;	\
	done

test-iomap-regression: exists.cs
	@$(MCS) $(srcdir)/exists.cs -out:exists.exe
	@echo "Testing exists.exe..."
	@MONO_IOMAP=all $(RUNTIME) exists.exe

stresstest: $(STRESS_TESTS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(STRESS_TESTS); do	\
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
	@$(with_mono_path) MONO_CFG_DIR=$(mono_cfg_dir) MONO_SHARED_DIR=$(mono_build_root)/runtime ./a.out >> mkbundle.stdout
	@- rm -rf a.out

EXTRA_DIST += load-missing.il t-missing.cs load-exceptions.cs
test-type-load: TestDriver.dll
	@$(ILASM) /dll /output:load-missing.dll $(srcdir)/load-missing.il > /dev/null
	@$(MCS) -t:library -out:t.dll -d:FOUND $(srcdir)/t-missing.cs
	@$(MCS) -r:TestDriver.dll -r:load-missing.dll -r:t.dll -out:load-exceptions.exe $(srcdir)/load-exceptions.cs
	@$(MCS) -t:library -out:t.dll $(srcdir)/t-missing.cs
	@echo "Testing load-exception.exe..."
	@$(RUNTIME) load-exceptions.exe > load-exceptions.exe.stdout 2> load-exceptions.exe.stderr


EXTRA_DIST += sgen-bridge.cs sgen-descriptors.cs sgen-gshared-vtype.cs sgen-bridge-major-fragmentation.cs sgen-domain-unload.cs

SGEN_TESTS =	\
	sgen-descriptors.exe	\
	sgen-gshared-vtype.exe	\
	sgen-domain-unload.exe

sgen-regular-tests: $(SGEN_TESTS)
	@for fn in $+ ; do	\
		echo "Testing $$fn ...";	\
		MONO_GC_PARAMS=major=marksweep-par MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) $$fn > $$fn.1.stdout 2> $$fn.1.stderr || exit 1;	\
		MONO_ENV_OPTIONS="--gc=sgen"                                    $(RUNTIME) $$fn > $$fn.2.stdout 2> $$fn.2.stderr || exit 1;	\
		MONO_GC_PARAMS=major=marksweep-par,minor=split MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) $$fn > $$fn.3.stdout 2> $$fn.3.stderr || exit 1;	\
		MONO_GC_PARAMS=minor=split MONO_ENV_OPTIONS="--gc=sgen"                     $(RUNTIME) $$fn > $$fn.4.stdout 2> $$fn.4.stderr || exit 1;	\
		MONO_GC_PARAMS=major=marksweep,concurrent-sweep MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) $$fn > $$fn.5.stdout 2> $$fn.5.stderr || exit 1;	\
		MONO_GC_PARAMS=minor=split,major=marksweep,concurrent-sweep MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) $$fn > $$fn.6.stdout 2> $$fn.6.stderr || exit 1;	\
		MONO_GC_PARAMS=major=marksweep-par,minor=split,alloc-ratio=95 MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) $$fn > $$fn.7.stdout 2> $$fn.7.stderr || exit 1;	\
		MONO_GC_PARAMS=minor=split,alloc-ratio=95 MONO_ENV_OPTIONS="--gc=sgen"                     $(RUNTIME) $$fn > $$fn.8.stdout 2> $$fn.8.stderr || exit 1;	\
	done


sgen-tests: sgen-regular-tests sgen-bridge.exe sgen-bridge-major-fragmentation.exe
	@echo "Testing sgen-bridge.exe ...";	\
	MONO_GC_PARAMS=bridge=Bridge MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) sgen-bridge.exe > sgen-bridge.exe.1.stdout 2> sgen-bridge.exe.1.stderr || exit 1;	\
	MONO_GC_PARAMS=bridge=Bridge,minor=split MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) sgen-bridge.exe > sgen-bridge.exe.2.stdout 2> sgen-bridge.exe.2.stderr || exit 1;	\
	echo "Testing sgen-bridge-major-fragmentation.exe ...";	\
	MONO_GC_PARAMS=bridge=Bridge MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) sgen-bridge-major-fragmentation.exe > sgen-bridge-major-fragmentation.exe.1.stdout 2> sgen-bridge-major-fragmentation.exe.1.stderr || exit 1;	\
	MONO_GC_PARAMS=bridge=Bridge,minor=split MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) sgen-bridge-major-fragmentation.exe > sgen-bridge-major-fragmentation.exe.2.stdout 2> sgen-bridge-major-fragmentation.exe.2.stderr || exit 1;


# Generated tests for runtime invoke
EXTRA_DIST += gen-runtime-invoke.cs
runtime-invoke.gen.exe: TestDriver.dll gen-runtime-invoke.exe
	$(RUNTIME) gen-runtime-invoke.exe > runtime-invoke.gen.cs
	$(MCS) -out:runtime-invoke.gen.exe -r:TestDriver.dll runtime-invoke.gen.cs

EXTRA_DIST += make-imt-test.cs
imt_big_iface_test.exe: TestDriver.dll make-imt-test.exe
	$(RUNTIME) make-imt-test.exe > imt_big_iface_test.cs
	$(MCS) -out:imt_big_iface_test.exe -r:TestDriver.dll imt_big_iface_test.cs

EXTRA_DIST += test-inline-call-stack-library.cs test-inline-call-stack.cs
test-inline-call-stack-library.dll: TestDriver.dll $(srcdir)/test-inline-call-stack-library.cs
	$(MCS) -t:library -out:test-inline-call-stack-library.dll $(srcdir)/test-inline-call-stack-library.cs
test-inline-call-stack.exe: TestDriver.dll test-inline-call-stack-library.dll $(srcdir)/test-inline-call-stack.cs
	$(MCS) -r:TestDriver.dll -r:test-inline-call-stack-library.dll -out:test-inline-call-stack.exe $(srcdir)/test-inline-call-stack.cs

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
unhandled-exception-test-case.1.exe: unhandled-exception-test-case.1.cs
	$(MCS) /debug unhandled-exception-test-case.1.cs
unhandled-exception-test-case-legacy.1.exe: unhandled-exception-test-case-legacy.1.cs
	$(MCS) /debug unhandled-exception-test-case-legacy.1.cs
unhandled-exception-test-case.2.exe: unhandled-exception-test-case.2.cs
	$(MCS) /debug unhandled-exception-test-case.2.cs
unhandled-exception-test-case-legacy.2.exe: unhandled-exception-test-case-legacy.2.cs
	$(MCS) /debug unhandled-exception-test-case-legacy.2.cs
unhandled-exception-test-runner.2.exe: unhandled-exception-test-runner.2.cs
	$(MCS) /debug unhandled-exception-test-runner.2.cs

test-unhandled-exception: unhandled-exception-test-runner.2.exe unhandled-exception-config_files unhandled-exception-test-case.1.exe unhandled-exception-test-case-legacy.1.exe  unhandled-exception-test-case.2.exe unhandled-exception-test-case-legacy.2.exe
	$(RUNTIME) unhandled-exception-test-runner.2.exe RUNTIME:../mini/mono,GTC:F

# We use 'test-support-files' to handle an ordering issue between the 'mono/' and 'runtime/' directories
bug-80307.exe: $(srcdir)/bug-80307.cs
	$(MCS) -r:System.Web -out:$@ $(srcdir)/bug-80307.cs
	cd $(top_builddir)/runtime && $(MAKE) test-support-files

EXTRA_DIST += bug-81673-interface.cs
bug-81673.exe bug-81673-interface.dll: $(srcdir)/bug-81673.cs $(srcdir)/bug-81673-interface.cs
	$(MCS) -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs
	$(MCS) -out:bug-81673.exe -r:bug-81673-interface.dll $(srcdir)/bug-81673.cs
	$(MCS) -define:WITH_STOP -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs

EXTRA_DIST += bug-81691-a.cs bug-81691-b.cs
bug-81691.exe bug-81691-b.dll: $(srcdir)/bug-81691.cs $(srcdir)/bug-81691-a.cs $(srcdir)/bug-81691-b.cs
	$(MCS) -target:library -out:bug-81691-a.dll $(srcdir)/bug-81691-a.cs
	$(MCS) -target:library -out:bug-81691-b.dll -r:bug-81691-a.dll $(srcdir)/bug-81691-b.cs
	$(MCS) -r:bug-81691-b.dll -out:bug-81691.exe $(srcdir)/bug-81691.cs
	rm -f bug-81691-a.dll
bug-81691.exe: bug-81691-b.dll

EXTRA_DIST += bug-81466-lib.il
bug-81466-lib.dll: bug-81466-lib.il
	$(ILASM) /dll /output:bug-81466-lib.dll $(srcdir)/bug-81466-lib.il
bug-81466.exe: bug-81466.il bug-81466-lib.dll
	$(ILASM) /exe /output:bug-81466.exe $(srcdir)/bug-81466.il

EXTRA_DIST += bug-324535-il.il
bug-324535-il.dll : bug-324535-il.il
	$(ILASM) /dll /output:bug-324535-il.dll $(srcdir)/bug-324535-il.il
bug-324535.exe : bug-324535.cs bug-324535-il.dll
	$(MCS) -r:bug-324535-il.dll -out:bug-324535.exe $(srcdir)/bug-324535.cs

EXTRA_DIST += custom-modifiers.2.cs custom-modifiers-lib.il
custom-modifiers-lib.dll: custom-modifiers-lib.il
	$(ILASM) /dll /output:custom-modifiers-lib.dll $(srcdir)/custom-modifiers-lib.il
custom-modifiers.2.exe: custom-modifiers.2.cs custom-modifiers-lib.dll
	$(MCS) -r:custom-modifiers-lib.dll -out:custom-modifiers.2.exe $(srcdir)/custom-modifiers.2.cs

EXTRA_DIST += bug-382986-lib.cs
bug-382986-lib.dll: bug-382986-lib.cs
	$(MCS) -target:library -out:$@ $(srcdir)/bug-382986-lib.cs
bug-382986.exe: bug-382986.cs bug-382986-lib.dll
	$(MCS) -out:$@ -r:bug-382986-lib.dll $(srcdir)/bug-382986.cs

EXTRA_DIST += coreclr-security.cs

if MOONLIGHT
coreclr-security.exe : coreclr-security.cs
	$(SMCS) -out:coreclr-security.exe $(srcdir)/coreclr-security.cs

test-coreclr-security : coreclr-security.exe
	@echo "Testing coreclr-security.exe..."
	@$(RUNTIME_MOONLIGHT) --security=core-clr-test coreclr-security.exe
endif

EXTRA_DIST += generic-unboxing.2.il
generic-unboxing.2.dll : generic-unboxing.2.il
	$(ILASM) /dll /output:generic-unboxing.2.dll $(srcdir)/generic-unboxing.2.il

EXTRA_DIST += generic-boxing.2.il
generic-boxing.2.dll : generic-boxing.2.il generic-unboxing.2.dll
	$(ILASM) /dll /output:generic-boxing.2.dll $(srcdir)/generic-boxing.2.il

EXTRA_DIST += generic-unbox.2.cs
generic-unbox.2.exe : generic-unbox.2.cs generic-unboxing.2.dll
	$(MCS) -r:generic-unboxing.2.dll -out:$@ $(srcdir)/generic-unbox.2.cs

EXTRA_DIST += generic-box.2.cs
generic-box.2.exe : generic-box.2.cs generic-unboxing.2.dll generic-boxing.2.dll
	$(MCS) -r:generic-unboxing.2.dll,generic-boxing.2.dll -out:$@ $(srcdir)/generic-box.2.cs

EXTRA_DIST += generic-delegate2.2.cs generic-delegate2-lib.2.il
generic-delegate2-lib.2.dll : generic-delegate2-lib.2.il
	$(ILASM) /dll /output:$@ $(srcdir)/generic-delegate2-lib.2.il
generic-delegate2.2.exe : generic-delegate2.2.cs generic-delegate2-lib.2.dll
	$(MCS) -r:generic-delegate2-lib.2.dll -out:$@ $(srcdir)/generic-delegate2.2.cs

bug-3903.exe: bug-3903.cs
	$(MCS) -sdk:2 $(srcdir)/bug-3903.cs -out:$@

gshared: test-generic-sharing

gshared-aot:
	@$(MAKE) AOT=1 gshared

GSHARED_TESTS = \
		generics-sharing.2.exe shared-generic-methods.2.exe	\
		shared-generic-synchronized.2.exe generic-initobj.2.exe		\
		generics-sharing-other-exc.2.exe generic-box.2.exe		\
		generic-unbox.2.exe generic-delegate.2.exe generic-sizeof.2.exe	\
		generic-ldobj.2.exe generic-mkrefany.2.exe			\
		generic-refanyval.2.exe generic-ldtoken.2.exe			\
		generic-ldtoken-method.2.exe  generic-ldtoken-field.2.exe	\
		generic-virtual.2.exe generic-tailcall.2.exe			\
		generic-interface-methods.2.exe generic-array-type.2.exe	\
		generic-method-patching.2.exe generic-static-methods.2.exe	\
		generic-null-call.2.exe generic-tailcall2.2.exe			\
		generic-array-exc.2.exe generic-special.2.exe			\
		generic-exceptions.2.exe generic-delegate2.2.exe		\
		generic-virtual2.2.exe generic-valuetype-interface.2.exe	\
		generic-valuetype-newobj.2.exe generic-valuetype-newobj2.2.exe	\
		generic-getgenericarguments.2.exe generic-type-builder.2.exe	\
		generic-synchronized.2.exe generic-delegate-ctor.2.exe		\
		generic-constrained.2.exe bug-431413.2.exe			\
		generic-virtual-invoke.2.exe generic-typedef.2.exe		\
		generic-marshalbyref.2.exe bug-459285.2.exe bug-461198.2.exe	\
		generic-sealed-virtual.2.exe generic-system-arrays.2.exe	\
		generic-stack-traces.2.exe generic-stack-traces2.2.exe		\
		bug-472600.2.exe bug-473482.2.exe bug-473999.2.exe		\
		bug-479763.2.exe generic-xdomain.2.exe				\
		generic-type-load-exception.2.exe bug-616463.exe	\
		bug-1147.exe

test-generic-sharing-normal: $(GSHARED_TESTS)
	@for fn in $+ ; do	\
		echo "Testing $$fn ...";	\
		MONO_GENERIC_SHARING=all $(RUNTIME) -O=gshared                $$fn > $$fn.stdout || exit 1;	\
		MONO_GENERIC_SHARING=all $(RUNTIME) -O=gshared,shared         $$fn > $$fn.stdout || exit 1;	\
		MONO_GENERIC_SHARING=all $(RUNTIME) -O=gshared,-inline        $$fn > $$fn.stdout || exit 1;	\
		MONO_GENERIC_SHARING=all $(RUNTIME) -O=gshared,-inline,shared $$fn > $fn.stdout || exit 1;	\
		if [ x$(AOT) = x1 ]; then MONO_GENERIC_SHARING=all $(with_mono_path) $(JITTEST_PROG_RUN) --aot --debug $$fn > /dev/null || exit 1; MONO_GENERIC_SHARING=all $(RUNTIME) $$fn > $$fn.stdout || exit 1; fi; \
	done

test-generic-sharing-managed: test-runner.exe $(GSHARED_TESTS)
	@$(RUNTIME) ./test-runner.exe -j a --opt-sets "gshared gshared,shared gshared,-inline gshared,-inline,shared" $(GSHARED_TESTS)

test-generic-sharing:
	@if test x$(M) != x; then $(MAKE) test-generic-sharing-managed; else $(MAKE) test-generic-sharing-normal; fi

EXTRA_DIST += async-exceptions.cs
async-exceptions.exe : async-exceptions.cs
	$(MCS) -out:async-exceptions.exe $(srcdir)/async-exceptions.cs
test-async-exceptions : async-exceptions.exe
	for i in `echo 0 1 2 3 4 5 6 7 8 9 10`; do $(RUNTIME) --inject-async-exc Tests:foo $$i async-exceptions.exe || exit 1; done
	for i in `echo 0 1 2 3 4 5 6 7 8 9 10`; do $(RUNTIME) --inject-async-exc Tests:bar $$i async-exceptions.exe || exit 1; done

EXTRA_DIST += modules.cs modules-m1.cs
modules-m1.netmodule: modules-m1.cs
	$(MCS) -out:$@ /target:module $(srcdir)/modules-m1.cs
modules.exe: modules.cs modules-m1.netmodule TestDriver.dll
	$(MCS) -out:$@ /addmodule:modules-m1.netmodule -r:TestDriver.dll $(srcdir)/modules.cs

# Useful if mono is compiled with --enable-shared=no
patch-libtool:
	cp "../../libtool" .
	sed -e 's,build_libtool_libs=no,build_libtool_libs=yes,g' libtool > 2; mv 2 libtool
	sed -e 's,LIBTOOL =,LIBTOOL2 =,g' Makefile > 2 && echo "LIBTOOL = bash ./libtool" > 1 && cat 1 2 > Makefile
	touch libtest.c

EXTRA_DIST += bug-438454.cs bug-438454.exe.stdout.expected
test-process-exit:
	@$(MCS) $(srcdir)/bug-438454.cs -out:bug-438454.exe
	@echo "Testing bug-438454.exe..."
	@$(RUNTIME) bug-438454.exe > bug-438454.exe.stdout
	@diff -w bug-438454.exe.stdout $(srcdir)/bug-438454.exe.stdout.expected

OOM_TESTS =	\
	gc-oom-handling.exe	\
	gc-oom-handling2.exe


test-oom: $(OOM_TESTS)
	@for fn in $+ ; do	\
		echo "Testing $$fn ...";	\
		MONO_GC_PARAMS=max-heap-size=16m,major=marksweep-par MONO_ENV_OPTIONS="--gc=sgen" $(RUNTIME) $$fn > $$fn.stdout || exit 1;	\
		MONO_GC_PARAMS=max-heap-size=16m MONO_ENV_OPTIONS="--gc=sgen"                     $(RUNTIME) $$fn > $$fn.stdout || exit 1;	\
		MONO_GC_PARAMS=max-heap-size=16m                                                  $(RUNTIME) $$fn > $$fn.stdout || exit 1;	\
	done


noinst_LTLIBRARIES = libtest.la

AM_CPPFLAGS = $(GLIB_CFLAGS) $(GMODULE_CFLAGS)

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

CLEANFILES = $(TESTSI_CS) $(TESTSI_IL) $(STRESS_TESTS) *.dll *.stdout *.aotlog *.exe stest.dat
