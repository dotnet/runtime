with_mono_path = MONO_PATH=$(mcs_topdir)/class/lib/default

RUNTIME = $(with_mono_path) $(top_builddir)/runtime/mono-wrapper --debug --security
MCS = $(RUNTIME) $(mcs_topdir)/class/lib/default/mcs.exe -debug:full -target:library

all: prereq test/asm.dll

test/asm.dll: deps/test.dll asm.cs
	$(MCS) -out:test/asm.dll -r:deps/test.dll asm.cs

deps/test.dll: deps/TestBase.dll Test.cs
	$(MCS) -out:deps/test.dll -r:deps/TestBase.dll Test.cs

deps/TestBase.dll: TestBase.cs
	$(MCS) -out:deps/TestBase.dll TestBase.cs

prereq:
	@-mkdir test
	@-mkdir deps
