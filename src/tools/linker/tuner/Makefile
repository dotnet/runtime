thisdir = tools/tuner
SUBDIRS =
include ../../build/rules.make

LINKER = ../linker/monolinker.exe
MASTER_INFOS_ZIP = http://mono.ximian.com/masterinfos/masterinfo-sl11a-refresh.zip

TUNER_SOURCES = \
	Mono.Tuner/PrintStatus.cs	\
	Mono.Tuner/RemoveSerialization.cs	\
	Mono.Tuner/CheckVisibility.cs	\

TUNER_DESCS =	\
	Descriptors/mscorlib.xml	\
	Descriptors/smcs.xml	\
	Descriptors/System.xml	\

LOCAL_MCS_FLAGS = -r:$(LINKER) -r:Mono.Cecil.dll

DISTFILES = README $(TUNER_SOURCES) $(TUNER_DESCS)

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
	rm -rf masterinfos*

endif

uninstall-local:

dist-local: dist-default

test-local:

run-test-local run-test-ondotnet-local:

MCS_MASTER_INFOS =	\
	masterinfos/silverlight/mscorlib.info	\
	masterinfos/silverlight/System.info		\
	masterinfos/silverlight/System.Core.info	\
	masterinfos/silverlight/System.Xml.Core.info	\

$(MCS_MASTER_INFOS):
	wget -O masterinfos.zip $(MASTER_INFOS_ZIP)
	unzip masterinfos.zip

TUNER_FLAGS = -d $(WORKING_DIR) -o $(OUTPUT_DIR) -l none -c link -a smcs -b true
TUNER_STEPS = \
	Mono.Tuner.PrintStatus,Mono.Tuner:OutputStep	\
	Mono.Tuner.RemoveSerialization,Mono.Tuner:OutputStep	\
	Mono.Tuner.CheckVisibility,Mono.Tuner	\

tune: $(MCS_MASTER_INFOS)
	MONO_PATH=".$(PLATFORM_PATH_SEPARATOR)$(topdir)/class/lib/default$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(LINKER) $(TUNER_FLAGS) $(TUNER_DESCS:%=-x %) $(TUNER_STEPS:%=-s %) $(MCS_MASTER_INFOS:%=-i %)

gacutil = $(topdir)/class/lib/net_1_1_bootstrap/gacutil.exe
GACUTIL = MONO_PATH="$(topdir)/class/lib/net_1_1_bootstrap$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(gacutil)

end-install: inst-mscorlib.dll inst-smcs.exe gac-System gac-System.Xml.Core gac-System.Core

inst-%:
	$(INSTALL_LIB) $(OUTPUT_DIR)/$* $(mono_libdir)/mono/2.1/$*
	test ! -f $(OUTPUT_DIR)/$*.mdb || $(INSTALL_LIB) $(OUTPUT_DIR)/$*.mdb $(mono_libdir)/mono/2.1/$*.mdb

gac-%:
	$(GACUTIL) /i $(OUTPUT_DIR)/$*.dll /f /root $(mono_libdir) /package 2.1

Mono.Tuner.dll:
	$(CSCOMPILE) /out:Mono.Tuner.dll /t:library $(TUNER_SOURCES)
