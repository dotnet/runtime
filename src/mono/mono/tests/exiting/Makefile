check-local: test

mcs_topdir = ../../../../mcs
mono_build_root = ../../..
top_builddir = $(mono_build_root)
srcdir = .
TEST_PROG = ../../interpreter/mint
JITTEST_PROG = ../../mini/mono

TEST_PROG_RUN = MONO_PATH=$(mcs_topdir)/class/lib/default $(TEST_PROG)
JITTEST_PROG_RUN = MONO_PATH=$(mcs_topdir)/class/lib/default $(JITTEST_PROG)

RUNTIME_ARGS=--config tests-config --optimize=all

RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/default $(top_builddir)/runtime/mono-wrapper

CSC = $(RUNTIME) $(mcs_topdir)/class/lib/default/mcs.exe --unsafe -nowarn:0162
ILASM = $(RUNTIME) $(mcs_topdir)/ilasm/ilasm.exe

# notes on programs that fail:
# delegate-async-exception.cs returns 1 on mono, 0 on MS 1.1
# delegate-delegate-exception.cs returns 1 on mono, hangs on MS 1.1
# finalizer-exception.cs returns 42 on mono and MS 1.1: on MS prints stack trace
#
# changing to run in the main thread:
# delegate-async-exception.cs hangs
# delegate-delegate-exception.cs hangs
# finalizer-exception.cs same as above
# main-returns-abort-resetabort.exe crashes randomly
# main-returns.exe doesn't wait the threads

#	delegate-async-exception.cs	\
#	delegate-delegate-exception.cs	\

TEST_CS_SRC=			\
	appdomain.cs	\
	delegate-async-exit.cs	\
	delegate-delegate-exit.cs	\
	delegate.cs	\
	finalizer-abort.cs	\
	finalizer-exception.cs	\
	finalizer-exit.cs	\
	main-exit.cs	\
	main-returns-abort-resetabort.cs	\
	main-returns-background-abort-resetabort.cs	\
	main-returns-background-resetabort.cs	\
	main-returns-background.cs	\
	main-returns.cs	\
	subthread-exit.cs

DISABLED_TESTS=			\
	$(PLATFORM_DISABLED_TESTS)

TESTSI_CS=$(TEST_CS_SRC:.cs=.exe)

%.exe: %.cs 
	$(CSC) -out:$@ $<

test:	testjit

test_cs: $(TEST_PROG) $(TESTSI_CS)
	@failed=0; \
	passed=0; \
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS); do	\
		if $(srcdir)/../test-driver '$(TEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo; echo ".cs: $${passed} test(s) passed. $${failed} test(s) failed."; echo

testjit: $(TESTSI_CS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	for i in $(TESTSI_CS); do	\
		rm -f $${i}.so; \
		if $(srcdir)/../test-driver '$(JITTEST_PROG_RUN)' $$i '$(DISABLED_TESTS)' $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."; \
	if [ $${failed} != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

testinterp: $(TESTSI_CS)
	@failed=0; \
	passed=0; \
	failed_tests="";\
	export LD_LIBRARY_PATH="`pwd`/.libs:$${LD_LIBRARY_PATH}"; \
	for i in $(TESTSI_CS); do \
		if $(srcdir)/../test-driver '$(TEST_PROG_RUN)' $$i "$(DISABLED_TESTS)" $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			if [ $$? = 2 ]; then break; fi; \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."; \
	if [ $$failed != 0 ]; then echo -e "\nFailed tests:\n"; \
	  for i in $${failed_tests}; do echo $${i}; done; exit 1; fi

CLEANFILES = $(TESTSI_CS) $(TESTSI_IL) *.dll *.stdout *.exe stest.dat
