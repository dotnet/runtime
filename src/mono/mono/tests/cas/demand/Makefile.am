with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug

CAS_RUNTIME = $(RUNTIME) --security

CSCOMPILE = $(RUNTIME) $(mcs_topdir)/class/lib/basic/mcs.exe -debug
PROFILE = net_2_0

EXTRA_DIST = pinvoke1.cs pinvoke2.cs pinvoke3.cs \
	sucs1.cs sucs2.cs sucs3.cs sucs4.cs \
	selfassert.cs selfdeny.cs selfpermit.cs

tests:	pinvoke1.exe pinvoke2.exe pinvoke3.exe \
	sucs1.exe sucs2.exe sucs3.exe sucs4.exe \
	selfassert.exe selfdeny.exe selfpermit.exe

aot:	pinvoke1.exe.so pinvoke2.exe.so pinvoke3.exe.so \
	sucs1.exe.so sucs2.exe.so sucs3.exe.so sucs4.exe.so \
	selfassert.exe.so selfdeny.exe.so selfpermit.exe.so

FULLTRUST_TEST_FILES = pinvoke1 pinvoke2 pinvoke3 \
	sucs1 sucs2 sucs3 sucs4 \
	selfassert selfdeny selfpermit

UNHANDLED_TEST_FILES = 

run: tests
	@for i in $(FULLTRUST_TEST_FILES); do	\
		$(CAS_RUNTIME) $$i.exe;	\
	done;
	@for i in $(INTERNET_TEST_FILES); do	\
		MONO_CAS_ZONE=Internet $(CAS_RUNTIME) $$i.exe;	\
	done;

test: tests
	@failed=0; \
	passed=0; \
	for i in $(FULLTRUST_TEST_FILES); do	\
		$(CAS_RUNTIME) $$i.exe > /dev/null;	\
		if [ "$$?" = "0" ]; then	\
			echo -e "fulltrust-$$i\tpass";	\
			passed=`expr $${passed} + 1`; \
		else	\
			echo -e "fulltrust-$$i\tFAIL ($$?)";	\
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} fulltrust-$$i"; \
		fi;	\
	done;	\
	for i in $(UNHANDLED_TEST_FILES); do	\
		$(CAS_RUNTIME) $$i.exe > /dev/null;	\
		if [ "$$?" = "0" ]; then	\
			echo -e "unhandled-$$i\tFAIL ($$?)";	\
			failed=`expr $${failed} + 1`; \
			failed_tests="$${failed_tests} unhandled-$$i"; \
		else	\
			echo -e "unhandled-$$i\tpass";	\
			passed=`expr $${passed} + 1`; \
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
	rm -f *.exe* *.dll* *.snk

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

%.exe.so: %.exe
	$(RUNTIME) --aot $^
