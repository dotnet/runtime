SUBDIRS = cas assemblyresolve

# avoid including eglib twice since GLIB_LIBS == GMODULE_LIBS for eglib builds
if EGLIB_BUILD
glib_libs = $(GLIB_LIBS)
else
glib_libs = $(GLIB_LIBS) $(GMODULE_LIBS)
endif

check-local: test

TEST_PROG = ../interpreter/mint
JITTEST_PROG = ../mini/mono

TEST_PROG_RUN = MONO_SHARED_DIR=$(mono_build_root)/runtime MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(TEST_PROG)
JITTEST_PROG_RUN = MONO_SHARED_DIR=$(mono_build_root)/runtime MONO_CFG_DIR=$(mono_build_root)/runtime/etc $(LIBTOOL) --mode=execute $(JITTEST_PROG)

RUNTIME_ARGS=--config tests-config --optimize=all

with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0
with_mono_path21 = MONO_PATH=$(mcs_topdir)/class/lib/net_2_1

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper
RUNTIME21 = $(with_mono_path21) $(top_builddir)/runtime/mono-wrapper

MKBUNDLE = \
	PKG_CONFIG_PATH=$(top_builddir):$(PKG_CONFIG_PATH) \
	$(RUNTIME) $(mcs_topdir)/class/lib/net_2_0/mkbundle.exe

MCS = $(RUNTIME) $(mcs_topdir)/class/lib/net_2_0/gmcs.exe -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219
SMCS = $(RUNTIME21) --runtime=moonlight --security=temporary-smcs-hack $(mcs_topdir)/class/lib/net_2_1/smcs.exe -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219
ILASM = $(RUNTIME) $(mcs_topdir)/class/lib/net_2_0/ilasm.exe

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
	exit-stress.cs

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
	virtual-sync.cs	\
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
	bug-331958.cs		\
	interlocked.cs		\
	cross-domain.cs		\
	appdomain-exit.cs	\
	delegate-async-exit.cs	\
	delegate-delegate-exit.cs	\
	delegate-exit.cs	\
	finalizer-abort.cs	\
	finalizer-exception.cs	\
	finalizer-exit.cs	\
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
	imt_big_iface_test.cs \
	dbnull-missing.cs	\
	test-type-ctor.cs 	\
	soft-float-tests.cs	\
	thread-exit.cs		\
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
	generic-null-call.2.cs	\
	recursive-generics.2.cs	\
	bug-80392.2.cs		\
	dynamic-method-access.2.cs	\
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
	winx64structs.cs

if AMD64
TEST_CS_SRC = $(BASE_TEST_CS_SRC) async-exc-compilation.cs
else
if X86
TEST_CS_SRC = $(BASE_TEST_CS_SRC) async-exc-compilation.cs
else
TEST_CS_SRC = $(BASE_TEST_CS_SRC)
endif
endif

if IA64
TEST_CS_SRC+=async-exc-compilation.cs
endif

if X86
# bug #71274
PLATFORM_DISABLED_TESTS=finalizer-abort.exe finalizer-exception.exe finalizer-exit.exe

if PLATFORM_WIN32
PLATFORM_DISABLED_TESTS+=async-exc-compilation.exe
endif

endif

if POWERPC
# bug #71274
PLATFORM_DISABLED_TESTS=finalizer-abort.exe finalizer-exception.exe finalizer-exit.exe
endif

DISABLED_TESTS=			\
	delegate-async-exception.exe	\
	delegate-delegate-exception.exe	\
	appdomain-unload.exe		\
	bug-348522.2.exe	\
	$(PLATFORM_DISABLED_TESTS)

# These only compile with MS CSC
TEST_CSC_SRC=			\
	vararg.cs

TEST_IL_SRC=			\
	field-access.il		\
	method-access.il	\
	ldftn-access.il		\
	cpblkTest.il		\
	vbinterface.il		\
	jmpTest.il		\
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
	generic-array-exc.2.il

#	bug-318677.il

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

EXTRA_DIST=test-driver $(TEST_CS_SRC) $(TEST_IL_SRC) \
	$(BENCHSRC) $(STRESS_TESTS_SRC) stress-runner.pl $(PREREQ_IL_SRC) $(PREREQ_CS_SRC)

%.exe: %.il
	$(ILASM) -out:$@ $<

%.exe: %.cs TestDriver.dll
	$(MCS) -r:TestDriver.dll -out:$@ $<

# mkbundle works on ppc, but the pkg-config POC doesn't when run with make test
if POWERPC
test:	assemblyresolve/test/asm.dll testjit test-type-load test-inline-call-stack test-bug-80307 test-bug-81673 test-bug-81691 test-bug-81466 test-bug-324535 test-generic-sharing test-bug-382986
else
# Can't use mkbundle on win32 since there is no static build there
# Can't run test-unhandled-exception on Windows because of all the debug popups...
if PLATFORM_WIN32
test:   assemblyresolve/test/asm.dll testjit test-type-load test-inline-call-stack test-bug-80307 test-bug-81673 test-bug-81691 test-bug-81466 test-bug-324535 test-generic-sharing test-bug-382986
else
test:	assemblyresolve/test/asm.dll testjit testbundle test-type-load test-inline-call-stack test-iomap-regression test-bug-80307 test-bug-81673 test-bug-81691 test-bug-81466 test-bug-324535 custom-modifiers test-generic-sharing test-bug-382986 
endif
endif

if INSTALL_2_1
test : test-coreclr-security
endif

assemblyresolve/test/asm.dll:
	$(MAKE) -C assemblyresolve prereq

TestDriver.dll:
	$(MCS) -target:library -out:$@ $(srcdir)/../mini/TestDriver.cs

test_cs: $(TEST_PROG) $(TESTSI_CS) libtest.la
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
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
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
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

testjit: $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la $(PREREQSI_IL) $(PREREQSI_CS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTSI_CS) $(TESTBS) $(TESTSI_IL); do	\
		rm -f $${i}.so; \
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

testaot: $(TESTSI_CS) $(TESTSI_IL) $(TESTBS) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS) $(TESTSI_IL) $(TESTBS); do	\
		rm -f $${i}.so; \
		$(JITTEST_PROG_RUN) --aot $${i} > /dev/null; \
		if $(srcdir)/test-driver '$(with_mono_path) $(JITTEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $$failed != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testtrace:
	$(MAKE) RUNTIME_ARGS="$${RUNTIME_ARGS} --trace" testjit

testinterp: $(TESTSI_CS) $(TESTSI_IL) libtest.la
	@failed=0; \
	passed=0; \
	failed_tests="";\
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS) $(TESTSI_IL); do \
		if $(srcdir)/test-driver '$(with_mono_path) $(TEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	if [ $$failed != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testjitspeed: $(JITTEST_PROG) $(TESTBS)
	for i in $(TESTBS); do	\
		echo $$i;	\
		time $(JITTEST_PROG) $$i;	\
	done

test-iomap-regression: exists.cs
	$(MCS) $(srcdir)/exists.cs -out:exists.exe
	MONO_IOMAP=all $(RUNTIME) exists.exe

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
	$(MKBUNDLE) --static console.exe
	$(with_mono_path) MONO_CFG_DIR=$(mono_cfg_dir) MONO_SHARED_DIR=$(mono_build_root)/runtime ./a.out
	- rm -rf a.out

EXTRA_DIST += load-missing.il t-missing.cs load-exceptions.cs
test-type-load: TestDriver.dll
	$(ILASM) /dll /output:load-missing.dll $(srcdir)/load-missing.il
	$(MCS) -t:library -out:t.dll -d:FOUND $(srcdir)/t-missing.cs
	$(MCS) -r:TestDriver.dll -r:load-missing.dll -r:t.dll -out:load-exceptions.exe $(srcdir)/load-exceptions.cs
	$(MCS) -t:library -out:t.dll $(srcdir)/t-missing.cs
	$(RUNTIME) load-exceptions.exe

EXTRA_DIST += test-inline-call-stack-library.cs test-inline-call-stack.cs
test-inline-call-stack-library.dll: TestDriver.dll $(srcdir)/test-inline-call-stack-library.cs
	$(MCS) -t:library -out:test-inline-call-stack-library.dll $(srcdir)/test-inline-call-stack-library.cs
test-inline-call-stack.exe: TestDriver.dll test-inline-call-stack-library.dll $(srcdir)/test-inline-call-stack.cs
	$(MCS) -r:TestDriver.dll -r:test-inline-call-stack-library.dll -out:test-inline-call-stack.exe $(srcdir)/test-inline-call-stack.cs
test-inline-call-stack: TestDriver.dll test-inline-call-stack-library.dll test-inline-call-stack.exe
	$(RUNTIME) test-inline-call-stack.exe


EXTRA_DIST += unhandled-exception-base-configuration.config
EXTRA_DIST += unhandled-exception-legacy-configuration.config
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

EXTRA_DIST += bug-80307.cs
bug-80307.exe: $(srcdir)/bug-80307.cs
	$(MCS) -r:System.Web -out:$@ $(srcdir)/bug-80307.cs

# We use 'test-support-files' to handle an ordering issue between the 'mono/' and 'runtime/' directories
test-bug-80307: bug-80307.exe
	cd $(top_builddir)/runtime && $(MAKE) test-support-files
	$(RUNTIME) bug-80307.exe

EXTRA_DIST += bug-81673.cs bug-81673-interface.cs
bug-81673.exe bug-81673-interface.dll: $(srcdir)/bug-81673.cs $(srcdir)/bug-81673-interface.cs
	$(MCS) -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs
	$(MCS) -out:bug-81673.exe -r:bug-81673-interface.dll $(srcdir)/bug-81673.cs
	$(MCS) -define:WITH_STOP -target:library -out:bug-81673-interface.dll $(srcdir)/bug-81673-interface.cs

test-bug-81673 : bug-81673.exe bug-81673-interface.dll
	$(RUNTIME) bug-81673.exe

EXTRA_DIST += bug-81691.cs bug-81691-a.cs bug-81691-b.cs
bug-81691.exe bug-81691-b.dll: $(srcdir)/bug-81691.cs $(srcdir)/bug-81691-a.cs $(srcdir)/bug-81691-b.cs
	$(MCS) -target:library -out:bug-81691-a.dll $(srcdir)/bug-81691-a.cs
	$(MCS) -target:library -out:bug-81691-b.dll -r:bug-81691-a.dll $(srcdir)/bug-81691-b.cs
	$(MCS) -r:bug-81691-b.dll -out:bug-81691.exe $(srcdir)/bug-81691.cs
	rm -f bug-81691-a.dll

test-bug-81691 : bug-81691.exe bug-81691-b.dll
	$(RUNTIME) bug-81691.exe

EXTRA_DIST += bug-81466.il bug-81466-lib.il
bug-81466-lib.dll: bug-81466-lib.il
	$(ILASM) /dll /output:bug-81466-lib.dll $(srcdir)/bug-81466-lib.il
bug-81466.exe: bug-81466.il bug-81466-lib.dll
	$(ILASM) /exe /output:bug-81466.exe $(srcdir)/bug-81466.il
test-bug-81466: bug-81466.exe bug-81466-lib.dll
	$(RUNTIME) bug-81466.exe

EXTRA_DIST += bug-324535-il.il bug-324535.cs
bug-324535-il.dll : bug-324535-il.il
	$(ILASM) /dll /output:bug-324535-il.dll $(srcdir)/bug-324535-il.il
bug-324535.exe : bug-324535.cs bug-324535-il.dll
	$(MCS) -r:bug-324535-il.dll -out:bug-324535.exe $(srcdir)/bug-324535.cs
test-bug-324535 : bug-324535.exe bug-324535-il.dll
	$(RUNTIME) bug-324535.exe

EXTRA_DIST += make_imt_test.sh
imt_big_iface_test.cs: $(srcdir)/make_imt_test.sh
	$(srcdir)/make_imt_test.sh > imt_big_iface_test.cs

EXTRA_DIST += custom-modifiers.2.cs custom-modifiers-lib.il
custom-modifiers-lib.dll: custom-modifiers-lib.il
	$(ILASM) /dll /output:custom-modifiers-lib.dll $(srcdir)/custom-modifiers-lib.il
custom-modifiers.exe: custom-modifiers.2.cs custom-modifiers-lib.dll
	$(MCS) -r:custom-modifiers-lib.dll -out:custom-modifiers.exe $(srcdir)/custom-modifiers.2.cs
custom-modifiers: custom-modifiers.exe
	$(RUNTIME) custom-modifiers.exe

EXTRA_DIST += bug-382986.cs  bug-382986-lib.cs
bug-382986-lib.dll: bug-382986-lib.cs
	$(MCS) -target:library -out:$@ $(srcdir)/bug-382986-lib.cs
bug-382986.exe: bug-382986.cs bug-382986-lib.dll
	$(MCS) -out:$@ -r:bug-382986-lib.dll $(srcdir)/bug-382986.cs
test-bug-382986: bug-382986.exe
	$(RUNTIME) bug-382986.exe

EXTRA_DIST += coreclr-security.cs

if INSTALL_2_1
coreclr-security.exe : coreclr-security.cs
	$(SMCS) -out:coreclr-security.exe $(srcdir)/coreclr-security.cs

test-coreclr-security : coreclr-security.exe
	$(RUNTIME21) --security=core-clr-test coreclr-security.exe
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

test-generic-sharing : generics-sharing.2.exe shared-generic-methods.2.exe	\
		shared-generic-synchronized.2.exe generic-initobj.2.exe		\
		generics-sharing-other-exc.2.exe generic-box.2.exe		\
		generic-unbox.2.exe generic-delegate.2.exe generic-sizeof.2.exe	\
		generic-ldobj.2.exe generic-mkrefany.2.exe			\
		generic-refanyval.2.exe generic-ldtoken.2.exe			\
		generic-ldtoken-method.2.exe  generic-ldtoken-field.2.exe	\
		generic-virtual.2.exe generic-tailcall.2.exe			\
		generic-interface-methods.2.exe generic-array-type.2.exe	\
		generic-method-patching.2.exe	\
		generic-null-call.2.exe generic-tailcall2.2.exe			\
		generic-array-exc.2.exe
	for fn in $+ ; do	\
		echo "Testing $$fn ...";	\
		MONO_GENERIC_SHARING=all $(RUNTIME) -O=gshared,-inline        $$fn || exit 1;	\
		MONO_GENERIC_SHARING=all $(RUNTIME) -O=gshared,-inline,shared $$fn || exit 1;	\
	done

EXTRA_DIST += async-exceptions.cs
async-exceptions.exe : async-exceptions.cs
	$(MCS) -out:async-exceptions.exe $(srcdir)/async-exceptions.cs
test-async-exceptions : async-exceptions.exe
	for i in `echo 0 1 2 3 4 5 6 7 8 9 10`; do $(RUNTIME) --inject-async-exc Tests:foo $$i async-exceptions.exe || exit 1; done
	for i in `echo 0 1 2 3 4 5 6 7 8 9 10`; do $(RUNTIME) --inject-async-exc Tests:bar $$i async-exceptions.exe || exit 1; done

# Useful if mono is compiled with --enable-shared=no
patch-libtool:
	cp "../../libtool" .
	sed -e 's,build_libtool_libs=no,build_libtool_libs=yes,g' libtool > 2; mv 2 libtool
	sed -e 's,LIBTOOL =,LIBTOOL2 =,g' Makefile > 2 && echo "LIBTOOL = bash ./libtool" > 1 && cat 1 2 > Makefile
	touch libtest.c

noinst_LTLIBRARIES = libtest.la

INCLUDES = $(GLIB_CFLAGS) $(GMODULE_CFLAGS)

if PLATFORM_WIN32
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
libtest_la_LIBADD = $(glib_libs)

CLEANFILES = $(TESTSI_CS) $(TESTSI_IL) $(STRESS_TESTS) *.dll *.stdout *.exe stest.dat
