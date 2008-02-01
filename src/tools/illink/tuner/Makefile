thisdir = tools/tuner
SUBDIRS =
include ../../build/rules.make

LINKER = ../linker/monolinker.exe

TUNER_SOURCES = \
	Mono.Tuner/TunerAnnotations.cs	\
	Mono.Tuner/PrintStatus.cs	\
	Mono.Tuner/RemoveSerialization.cs	\
	Mono.Tuner/AdjustVisibility.cs	\
	Mono.Tuner/CheckVisibility.cs	\
	Mono.Tuner/InjectAttributes.cs	\

TUNER_DESCS =	\
	Descriptors/mscorlib.xml	\
	Descriptors/smcs.xml	\
	Descriptors/System.xml	\

TUNER_MASTERS =	\
	masterinfos/silverlight/mscorlib.info	\
	masterinfos/silverlight/System.info	\
	masterinfos/silverlight/System.Core.info	\
	masterinfos/silverlight/System.Xml.Core.info	\

LOCAL_MCS_FLAGS = -r:$(LINKER) -r:Mono.Cecil.dll

DISTFILES = README $(TUNER_SOURCES) $(TUNER_DESCS) $(TUNER_MASTERS)

WORKING_DIR = $(topdir)/class/lib/net_2_1
OUTPUT_DIR = $(topdir)/class/lib/net_2_1_tuned

ifneq (net_2_1, $(PROFILE))
all-local: Mono.Tuner.dll

install-local:

clean-local:
	rm -f *.dll *.mdb *.pdb

else
all-local: tune

install-local: end-install

clean-local:
	rm -rf $(OUTPUT_DIR)

endif

uninstall-local:

dist-local: dist-default

test-local:

run-test-local run-test-ondotnet-local:

TUNER_FLAGS = -d $(WORKING_DIR) -o $(OUTPUT_DIR)	\
	-l none		\
	-c link		\
	-a smcs		\
	-b true		\
	-m display_internalized false	\

TUNER_STEPS = \
	Mono.Tuner.InjectAttributes,Mono.Tuner:OutputStep	\
	Mono.Tuner.AdjustVisibility,Mono.Tuner:OutputStep	\
	Mono.Tuner.PrintStatus,Mono.Tuner:OutputStep	\
	Mono.Tuner.RemoveSerialization,Mono.Tuner:OutputStep	\
	Mono.Tuner.CheckVisibility,Mono.Tuner	\

tune:
	MONO_PATH=".$(PLATFORM_PATH_SEPARATOR)$(topdir)/class/lib/default$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) --debug $(LINKER) $(TUNER_FLAGS) $(TUNER_DESCS:%=-x %) $(TUNER_STEPS:%=-s %) $(TUNER_MASTERS:%=-i %)

gacutil = $(topdir)/class/lib/net_1_1_bootstrap/gacutil.exe
GACUTIL = MONO_PATH="$(topdir)/class/lib/net_1_1_bootstrap$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(gacutil)

TUNER_INSTALL_DIR = $(DESTDIR)$(mono_libdir)/mono/2.1

end-install: check-install-dir inst-mscorlib.dll inst-smcs.exe gac-System gac-System.Xml.Core gac-System.Core

check-install-dir:
	[ -d $(TUNER_INSTALL_DIR) ] || mkdir $(TUNER_INSTALL_DIR)

inst-%:
	$(INSTALL_LIB) $(OUTPUT_DIR)/$* $(TUNER_INSTALL_DIR)/$*
	test ! -f $(OUTPUT_DIR)/$*.mdb || $(INSTALL_LIB) $(OUTPUT_DIR)/$*.mdb $(TUNER_INSTALL_DIR)/$*.mdb

gac-%:
	$(GACUTIL) /i $(OUTPUT_DIR)/$*.dll /f /root $(DESTDIR)$(mono_libdir) /package 2.1

Mono.Tuner.dll:
	$(CSCOMPILE) /out:Mono.Tuner.dll /t:library $(TUNER_SOURCES)
