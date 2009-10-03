with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_2_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug

CAS_RUNTIME = $(RUNTIME) --security

PROFILE = net_2_0

compiler_defines = -d:NET_2_0

CSCOMPILE = $(RUNTIME) $(mcs_topdir)/class/lib/net_2_0/gmcs.exe -debug
SN = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug $(mcs_topdir)/tools/security/sn.exe

EXTRA_DIST = icall1.cs icall2.cs icall3.cs icall4.cs icall5.cs icall6.cs \
	aptc1.cs aptc2.cs aptclib.cs \
	pinvoke1.cs pinvoke2.cs pinvoke3.cs pinvoke4.cs \
	cas1.cs cas2.cs cas3.cs cas6.cs cas7.cs cas8.cs cas9.cs \
	refcas1.cs refcas2.cs refcas3.cs refcas4.cs refcas5.cs \
	noncas1.cs noncas2.cs noncas3.cs noncas4.cs

tests:	icall1.exe icall2.exe icall3.exe icall4.exe icall5.exe icall6.exe \
	aptc1a.exe aptc1b.exe aptc2a.exe aptc2b.exe aptclib.dll \
	pinvoke1.exe pinvoke2.exe pinvoke3.exe pinvoke4.exe \
	cas1.exe cas2.exe cas3.exe cas6.exe cas7.exe cas8.exe cas9.exe \
	refcas1.exe refcas2.exe refcas3.exe refcas4.exe refcas5.exe \
	noncas1.exe noncas2.exe noncas3.exe noncas4.exe

aot:	icall1.exe.so icall2.exe.so icall3.exe.so icall4.exe.so icall5.exe.so icall6.exe.so \
	aptc1a.exe.so aptc1b.exe.so aptc2a.exe.so aptc2b.exe.so aptclib.dll.so \
	pinvoke1.exe.so pinvoke2.exe.so pinvoke3.exe.so pinvoke4.exe.so \
	cas1.exe.so cas2.exe.so cas3.exe.so cas6.exe.so \
	cas7.exe.so cas8.exe.so cas8.exe.so cas9.exe.so \
	refcas1.exe.so refcas2.exe.so refcas3.exe.so refcas4.exe.so refcas5.exe.so \
	noncas1.exe.so noncas2.exe.so noncas3.exe.so noncas4.exe.so

FULLTRUST_TEST_FILES = icall1 icall2 icall3 icall4 icall6 \
	aptc1a aptc1b aptc2a \
	pinvoke1 pinvoke2 pinvoke4 \
	cas1 cas3 cas6 cas7 cas8 cas9 \
	refcas1 refcas2 refcas3 refcas4 \
	noncas1 noncas2 noncas3 noncas4

UNHANDLED_TEST_FILES = aptc2b cas2 pinvoke3 

run: tests
	@for i in $(FULLTRUST_TEST_FILES); do	\
		$(CAS_RUNTIME) $$i.exe;	\
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

aptc.snk:
	$(SN) -k aptc.snk

%a.exe: %.cs
	$(CSCOMPILE) $^ /out:$@ $(compiler_defines)

%b.exe: %.cs
	$(CSCOMPILE) $^ /out:$@ /keyfile:ecma.pub /delaysign+ $(compiler_defines)

aptc1b.exe: aptc1.cs aptc.snk
	$(CSCOMPILE) $< /out:$@ /d:RESTRICT $(compiler_defines)

aptc2a.exe: aptc2.cs aptclib.dll
	$(CSCOMPILE) $< /out:$@ /r:aptclib.dll $(compiler_defines)

aptc2b.exe: aptc2.cs aptclib.dll
	$(CSCOMPILE) $< /out:$@ /d:RESTRICT /r:aptclib.dll $(compiler_defines)

aptclib.dll: aptclib.cs aptc.snk
	$(CSCOMPILE) $< /out:$@ /target:library /keyfile:aptc.snk $(compiler_defines)

%.exe: %.cs
	$(CSCOMPILE) $^ /out:$@ $(compiler_defines)

%.exe.so: %.exe
	$(RUNTIME) --aot $^
