with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug

CAS_RUNTIME = $(RUNTIME) --security

CSCOMPILE = $(RUNTIME) $(mcs_topdir)/class/lib/net_2_0/gmcs.exe -debug
PROFILE = net_2_0

EXTRA_DIST = notused.cs cas1.cs cas2.cs cas3.cs \
	noncas1.cs noncas2.cs noncas3.cs noncas4.cs \
	refload1.cs refload2.cs refload3.cs refload4.cs \
	reftype1.cs reftype2.cs reftype3.cs reftype4.cs \
	library1.cs library2.cs cas.snk

tests:	notused.exe cas1a.exe cas2a.exe cas3a.exe \
	cas1b.exe cas2b.exe cas3b.exe \
	noncas1.exe noncas2.exe noncas3.exe noncas4.exe \
	refload1.exe refload2.exe refload3.exe refload4.exe \
	reftype1.exe reftype2.exe reftype3.exe reftype4.exe \
	library1a.dll library1b.dll library2a.dll library2b.dll

aot:	notused.exe.so cas1a.exe.so cas2a.exe.so cas3a.exe.so \
	cas1b.exe.so cas2b.exe.so cas3b.exe.so \
	noncas1.exe.so noncas2.exe.so noncas3.exe.so noncas4.exe.so \
	refload1.exe.so refload2.exe.so refload3.exe.so refload4.exe.so \
	reftype1.exe.so reftype2.exe.so reftype3.exe.so reftype4.exe.so \
	library1a.dll.so library1b.dll.so library2a.dll.so library2b.dll.so

FULLTRUST_TEST_FILES = notused cas1b cas2b cas3b noncas2 noncas4 \
	refload1 refload2 refload3 refload4 \
	reftype1 reftype2 reftype3 reftype4

UNHANDLED_TEST_FILES = cas1a cas2a cas3a noncas1 noncas3

INTERNET_TEST_FILES = 

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
	rm -f *.exe* *.dll*

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@

%a.dll: %.cs
	$(CSCOMPILE) /target:library $< /out:$@

%b.dll: %.cs 
	$(CSCOMPILE) /target:library $< /out:$@ /keyfile:$</../cas.snk

%a.exe: %.cs 
	$(CSCOMPILE) $< /out:$@

%b.exe: %.cs
	$(CSCOMPILE) $< /out:$@ /keyfile:$</../cas.snk

%.exe.so: %.exe
	$(RUNTIME) --aot $^
