with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/net_4_0

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug
MCS = $(RUNTIME) $(mcs_topdir)/class/lib/build/mcs.exe -debug:full -target:library

prereq: test/asm.dll

test/.dirstamp deps/.dirstamp:
	-mkdir $(@D)
	rm -f $@ && date > $@

test/asm.dll: test/.dirstamp
deps/test.dll deps/TestBase.dll: deps/.dirstamp

test/asm.dll: deps/test.dll asm.cs
	$(MCS) -out:test/asm.dll -r:deps/TestBase.dll -r:deps/test.dll $(srcdir)/asm.cs

deps/test.dll: deps/TestBase.dll Test.cs
	$(MCS) -out:deps/test.dll -r:deps/TestBase.dll $(srcdir)/Test.cs

deps/TestBase.dll: TestBase.cs
	$(MCS) -out:deps/TestBase.dll $(srcdir)/TestBase.cs

EXTRA_DIST = asm.cs Test.cs TestBase.cs

clean:
	rm -f deps/*.dll test/*.dll
