with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug

CAS_RUNTIME = $(RUNTIME) --security

CSCOMPILE = $(RUNTIME) $(mcs_topdir)/class/lib/basic/mcs.exe -debug
PERMVIEW = $(RUNTIME) $(mcs_topdir)/tools/security/permview.exe
PROFILE = net_2_0

tests: all.exe min.exe opt.exe ref.exe none.exe

aot: all.exe.so min.exe.so opt.exe.so ref.exe.so none.exe.so

TEST_FILES = all min opt ref none
EXTRA_DIST=all.cs min.cs opt.cs ref.cs none.cs \
	all.net_1_1.txt min.net_1_1.txt opt.net_1_1.txt ref.net_1_1.txt none.net_1_1.txt

# Only make sense on a 2.0 runtime
run: all
	@for i in $(TEST_FILES); do	\
		$(CAS_RUNTIME) $$i.exe;	\
	done;

# Templates are checked into SVN, for each profile, to detect regressions
# That's why they aren't removed by "make clean"
templates: all
	@for i in $(TEST_FILES); do	\
		$(PERMVIEW) /output $$i.$(PROFILE).txt $$i.exe > /dev/null;	\
	done;

test: tests
	@failed=0; \
	passed=0; \
	for i in $(TEST_FILES); do	\
		$(PERMVIEW) /output $$i.$(PROFILE).tmp $$i.exe > /dev/null;	\
		cmp -s $$i.$(PROFILE).tmp $$i.$(PROFILE).txt;	\
		if [ "$$?" = "0" ]; then	\
			echo -e "$$i\tpass";	\
			passed=`expr $${passed} + 1`; \
		else	\
			echo -e "$$i\tFAIL ($$?)";	\
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} $$i"; \
		fi;	\
	done;	\
	echo -e "\n$${passed} test(s) passed. $${failed} test(s) failed."; \
	if [ "$${failed}" != "0" ]; then	\
		echo -e "Failed tests are:";	\
		for i in $${failed_tests};	\
			do echo -e "\t$${i}";	\
		done;	\
		exit 1;	\
	fi

clean:
	rm -f *.exe* *.tmp

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

%.exe.so: %.exe
	$(RUNTIME) --aot $^
