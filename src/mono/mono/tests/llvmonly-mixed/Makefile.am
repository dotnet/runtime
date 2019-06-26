include ../common_mixed.mk

tests.exe: $(srcdir)/tests.cs TestDriver.dll interponly.dll
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -r:TestDriver.dll -r:interponly.dll -target:exe -out:$@  $<

interponly.dll: $(srcdir)/interponly.cs TestDriver.dll jitonly.dll
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Xml.dll -r:$(CLASS)/System.Core.dll -r:TestDriver.dll -r:jitonly.dll -target:library -out:$@ $<

jitonly.dll: $(srcdir)/jitonly.cs TestDriver.dll
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
ASSEMBLIES = mscorlib.dll tests.exe jitonly.dll TestDriver.dll

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

$(AOTDIR)/tests.exe: $(AOTDIR)/TestDriver.dll $(AOTDIR)/jitonly.dll $(AOTDIR)/interponly.dll

if ENABLE_LLVM
check: $(AOT_IMAGES)
	MONO_PATH=$(AOTDIR) $(MONO_EXE) --aot-path=$(AOTDIR) --llvmonly-interp $(AOTDIR)/tests.exe
endif

