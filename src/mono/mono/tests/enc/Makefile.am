include ../common_mixed.mk

#ROSLYNILDIFF_KIND=Mono
ROSLYNILDIFF_KIND=Net5

ROSLYNILDIFF ?=$(HOME)/work/roslynildiff/bin/Debug/roslynildiff.$(if $(findstring Net5,$(ROSLYNILDIFF_KIND)),dll,exe)

ROSLYNILDIFF_CMD =$(if $(findstring Mono,$(ROSLYNILDIFF_KIND)),mono --debug,dotnet)
ROSLYNILDIFF_RUN =$(ROSLYNILDIFF_CMD) $(ROSLYNILDIFF)

ROSLYNILDIFF_ARGS =$(if $(findstring Mono,$(ROSLYNILDIFF_KIND)),,-mono -bcl:$(CLASS))

DISABLED_TESTS := \
	AddClass.dll \
	AddStaticField.dll \
	LambdaFunc.dll

TESTS_REGULAR := \
	AddClass.dll \
	AddStaticField.dll \
	AddStaticMethod.dll \
	CallExisting.dll \
	LambdaFunc.dll \
	OnStackMethod.dll \
	OnStackMethodThread.dll \
	OnStackMethodThreadThread.dll \
	ReplaceMethod.dll \
	ReplaceMethodOften.dll \
	ReplacePrivateVirtualMethod.dll \
	TypeTokenSwap.dll \
	UserStringSwap.dll

TESTS_ACTUAL=$(filter-out $(DISABLED_TESTS),$(TESTS_REGULAR))

# DEBUG_FLAGS=MONO_LOG_MASK=metadata-update MONO_LOG_LEVEL=debug MONO_VERBOSE_METHOD=DiffTestMethod1

TESTS_BASE_SRCS:= $(TESTS_REGULAR:.dll=.cs)

TESTS_UPDATE_SRCS:= $(foreach b,$(basename $(TESTS_REGULAR)),$(wildcard $(b)_v?.cs))

TESTS_SRCS:= $(TEST_BASE_SRCS) $(TEST_UPDATE_SRCS)

EXTRA_DIST= $(TESTS_SRCS)

.PHONY: names

check-local: $(TESTS_REGULAR)
	@failed=0; \
	passed=0; \
	failed_tests=""; \
	for i in $(TESTS_ACTUAL); do \
		echo "======== $$i start ======== "; \
		if $(DEBUG_FLAGS) $(RUNTIME) $(TEST_RUNTIME_ARGS) --interp=-inline $$i ; \
		then \
			passed=`expr $${passed} + 1`; \
			echo "======== $$i end (passed) ======== "; \
		else \
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
			echo "======== $$i end (failed) ======== "; \
		fi;  \
	done; \
	echo "$${passed} test(s) passed. $${failed} test(s) did not pass."; \
	exit $${failed}

EXTRA_DIST+= EncHelper.cs

.PHONY: run-demo1 run-demo2

run-demo1: UserStringSwap.dll
	$(DEBUG_FLAGS) $(RUNTIME) $(TEST_RUNTIME_ARGS) --interp=-inline $<

run-demo2: ReplaceMethodOften.dll
	$(DEBUG_FLAGS) $(RUNTIME) $(TEST_RUNTIME_ARGS) --interp=-inline $<

EncHelper.dll: EncHelper.cs
	$(MCS)  -target:library -out:$@ $<

%.dll: %.cs EncHelper.dll
	bash -x -c "$(ROSLYNILDIFF_RUN) $(ROSLYNILDIFF_ARGS) -l:EncHelper.dll $< $(basename $<)_v?.cs"

CLEANFILES += *.dll.*.dil *.dll.*.dmeta *.dll.*.dpdb
