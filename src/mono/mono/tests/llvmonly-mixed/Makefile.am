
CLASS=$(mcs_topdir)/class/lib/$(DEFAULT_PROFILE)

with_mono_path = MONO_PATH=$(CLASS)

MONO_EXE = $(top_builddir)/runtime/mono-wrapper
RUNTIME = $(with_mono_path) $(MONO_EXE)
TOOLS_RUNTIME = MONO_PATH=$(mcs_topdir)/class/lib/build $(MONO_EXE) --aot-path=$(mcs_topdir)/class/lib/build

MCS_NO_UNSAFE = $(TOOLS_RUNTIME) $(CSC) -debug:portable \
	-noconfig -nologo \
	-nowarn:0162 -nowarn:0168 -nowarn:0219 -nowarn:0414 -nowarn:0618 \
	-nowarn:0169 -nowarn:1690 -nowarn:0649 -nowarn:0612 -nowarn:3021 \
	-nowarn:0197 -langversion:latest $(PROFILE_MCS_FLAGS)
MCS_NO_LIB = $(MCS_NO_UNSAFE) -unsafe

MCS = $(MCS_NO_LIB)

ILASM = $(TOOLS_RUNTIME) $(mcs_topdir)/class/lib/build/ilasm.exe

tests.exe: $(srcdir)/tests.cs TestDriver.dll interponly.dll
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -r:TestDriver.dll -r:interponly.dll -target:exe -out:$@  $<

interponly.dll: $(srcdir)/interponly.cs TestDriver.dll
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -r:TestDriver.dll -target:library -out:$@ $<

TestDriver.dll:
	$(MCS) -target:library -out:$@ $(srcdir)/../../mini/TestDriver.cs $(srcdir)/../../mini/TestHelpers.cs

AOTDIR=linker-output
AOT_TMPDIR=aot-tmp

$(AOTDIR)/tests.exe$(PLATFORM_AOT_SUFFIX): $(AOTDIR)/tests.exe
	mkdir -p $(AOT_TMPDIR)/tests.exe
	MONO_PATH=$(AOTDIR) $(MONO_EXE) --aot=llvmonly,interp,temp-path=$(AOT_TMPDIR)/tests.exe $(AOTDIR)/tests.exe

$(AOTDIR)/%.dll$(PLATFORM_AOT_SUFFIX): $(AOTDIR)/%.dll
	mkdir -p $(AOT_TMPDIR)/$*.dll
	MONO_PATH=$(AOTDIR) $(MONO_EXE) --aot=llvmonly,interp,temp-path=$(AOT_TMPDIR)/$*.dll $(AOTDIR)/$*.dll

SOURCES = \
	tests.cs \
	interponly.cs

# interponly.dll is not AOT-ed
ASSEMBLIES = mscorlib.dll tests.exe TestDriver.dll

AOT_IMAGES = $(patsubst %,$(AOTDIR)/%$(PLATFORM_AOT_SUFFIX),$(ASSEMBLIES))

$(AOTDIR)/mscorlib.dll:
	mkdir -p $(AOTDIR)
	cp $(CLASS)/mscorlib.dll $(AOTDIR)/

$(AOTDIR)/%.dll: %.dll
	mkdir -p $(AOTDIR)
	cp $< $@

$(AOTDIR)/%.exe: %.exe
	mkdir -p $(AOTDIR)
	cp $< $@

$(AOTDIR)/tests.exe: $(AOTDIR)/TestDriver.dll $(AOTDIR)/interponly.dll

if ENABLE_LLVM
check: $(AOT_IMAGES)
	MONO_PATH=$(AOTDIR) $(MONO_EXE) --aot-path=$(AOTDIR) --llvmonly-interp $(AOTDIR)/tests.exe
endif

CLEANFILES=*.dll *.exe *.pdb

clean-local:
	$(RM) -rf $(AOTDIR) $(AOT_TMPDIR)
