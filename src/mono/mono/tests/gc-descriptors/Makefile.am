with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_4_5
RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper
MCS = $(RUNTIME) $(mcs_topdir)/class/lib/build/mcs.exe -unsafe -nowarn:0162 -nowarn:0168 -nowarn:0219 -debug

check-local: test

test : descriptor-tests.exe

descriptor-tests.exe : descriptor-tests.cs
	$(MCS) -Warn:0 descriptor-tests.cs

descriptor-tests.cs : descriptor-tests-driver.cs descriptor-tests-prefix.cs gen-descriptor-tests.py
	-cp $^ .
	$(srcdir)/gen-descriptor-tests.py >descriptor-tests.cs

EXTRA_DIST = descriptor-tests-driver.cs descriptor-tests-prefix.cs gen-descriptor-tests.py
