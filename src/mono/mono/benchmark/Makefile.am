MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

TEST_PROG=../mini/mono
RUNTIME_ARGS="-O=all"

TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(top_builddir)/runtime/mono-wrapper --aot-path=$(mcs_topdir)/class/lib/build
MCS = $(TOOLS_RUNTIME) $(CSC) -noconfig -nologo -debug:portable -target:library $(PROFILE_MCS_FLAGS)
ILASM = $(TOOLS_RUNTIME) $(mcs_topdir)/class/lib/build/ilasm.exe

TESTSRC=			\
	fib.cs 			\
	life.cs 		\
	castclass.cs		\
	cmov1.cs		\
	cmov2.cs		\
	cmov3.cs		\
	cmov4.cs		\
	cmov5.cs		\
	commute.cs		\
	isinst.cs		\
	sbperf1.cs		\
	sbperf2.cs		\
	iconst-byte.cs		\
	inline1.cs		\
	inline2.cs		\
	inline3.cs		\
	inline4.cs		\
	inline5.cs		\
	inline6.cs		\
	inline-readonly.cs	\
	max-min.cs		\
	muldiv.cs		\
	loops.cs		\
	initlocals.cs		\
	logic.cs		\
	switch.cs		\
	ctor-bench.cs		\
	readonly.cs		\
	readonly-byte-array.cs  \
	readonly-inst.cs	\
	readonly-vt.cs		\
	regalloc.cs		\
	regalloc-2.cs		\
	bulkcpy.il		\
	math.cs			\
	boxtest.cs		\
	valuetype-hash-equals.cs \
	vt2.cs

TESTSI_TMP=$(TESTSRC:.cs=.exe)
TESTSI=$(TESTSI_TMP:.il=.exe)

EXTRA_DIST=test-driver $(TESTSRC)

%.exe: %.il
	$(ILASM) $< /OUTPUT=$@

%.exe: %.cs
	$(MCS) $< -out:$@

test-local: $(TEST_PROG) $(TESTSI)

run-test: $(TEST_PROG) $(TESTSI)
	@failed=0; \
	passed=0; \
	for i in $(TESTSI); do	\
		if ./test-driver $(TEST_PROG) $$i $(RUNTIME_ARGS); \
		then \
			passed=`expr $${passed} + 1`; \
		else \
			failed=`expr $${failed} + 1`; \
		fi \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) failed."

check:
	@echo no check yet
