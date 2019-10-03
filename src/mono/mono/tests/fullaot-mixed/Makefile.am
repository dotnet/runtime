include ../common_mixed.mk

lambda.exe: $(srcdir)/lambda.cs
	$(MCS) -r:$(CLASS)/System.dll -r:$(CLASS)/System.Core.dll -target:exe -out:$@ $<

AOTDIR=linker-output
AOT_TMPDIR=aot-tmp

if HOST_WIN32
PLATFORM_PATH_SEPARATOR=;
else
PLATFORM_PATH_SEPARATOR=:
endif

$(AOTDIR)/lambda.exe$(PLATFORM_AOT_SUFFIX): $(AOTDIR)/lambda.exe $(LINKED_ASSEMBLIES)
	mkdir -p $(AOT_TMPDIR)/lambda.exe
	MONO_PATH=$(AOTDIR) $(MONO_EXE) --aot=full,interp,temp-path=$(AOT_TMPDIR)/lambda.exe $(AOTDIR)/lambda.exe

$(AOTDIR)/%.dll$(PLATFORM_AOT_SUFFIX): $(AOTDIR)/%.dll $(LINKED_ASSEMBLIES)
	mkdir -p $(AOT_TMPDIR)/$*.dll
	MONO_PATH=$(AOTDIR) $(MONO_EXE) --aot=full,interp,temp-path=$(AOT_TMPDIR)/$*.dll $(AOTDIR)/$*.dll

SOURCES = \
	lambda.cs

ASSEMBLIES = \
	mscorlib.dll \
	System.Core.dll \
	System.dll \
	System.Security.dll \
	Mono.Simd.dll \
	Mono.Security.dll \
	System.Numerics.dll \
	System.Numerics.Vectors.dll \
	Mono.Posix.dll \
	System.Xml.dll \
	System.Configuration.dll

LINKED_ASSEMBLIES = $(addprefix $(AOTDIR)/,$(ASSEMBLIES))

AOT_IMAGES = $(patsubst %,$(AOTDIR)/%$(PLATFORM_AOT_SUFFIX),$(ASSEMBLIES))
TEST_IMAGES = $(patsubst %,$(AOTDIR)/%$(PLATFORM_AOT_SUFFIX),$(SOURCES:.cs=.exe))

$(AOTDIR)/%.dll:
	mkdir -p $(AOTDIR)
	cp $(CLASS)/$*.dll $(AOTDIR)/

$(AOTDIR)/%.exe: %.exe
	mkdir -p $(AOTDIR)
	cp $< $@

if !X86
if !DISABLE_INTERPRETER
if !ENABLE_COOP_SUSPEND
check: $(AOT_IMAGES) $(TEST_IMAGES)
	MONO_PATH="$(AOTDIR)$(PLATFORM_PATH_SEPARATOR)$(CLASS)" $(MONO_EXE) --aot-path=$(AOTDIR) --full-aot-interp $(AOTDIR)/lambda.exe
endif
endif
endif
