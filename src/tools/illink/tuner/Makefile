thisdir = tools/tuner
SUBDIRS =
include ../../build/rules.make

LINKER = ../linker/monolinker.exe
MASTER_INFOS_ZIP = http://mono.ximian.com/masterinfos/masterinfo-sl11a-refresh.zip

LINKER_PLUGINS = \
	Mono.Tuner/PrintStatus.dll	\
	Mono.Tuner/RemoveSerialization.dll	\
	Mono.Tuner/CheckVisibility.dll	\
#	Mono.Tuner/InjectAttributes.dll	\

LINKER_DESCS =	\
	Descriptors/mscorlib.xml	\
	Descriptors/smcs.xml	\
	Descriptors/System.xml	\

LOCAL_MCS_FLAGS = -r:$(LINKER) -r:Mono.Cecil.dll

LINKER_SOURCES = $(LINKER_PLUGINS:.dll=.cs)

DISTFILES = README $(LINKER_SOURCES) $(LINKER_DESCS)

WORKING_DIR = $(topdir)/class/lib/net_2_1
OUTPUT_DIR = $(topdir)/class/lib/net_2_1_tuned

ifneq (net_2_1, $(PROFILE))
all-local: $(LINKER_PLUGINS)

install-local:

clean-local:
	rm -f Mono.Tuner/*.dll
	rm -f Mono.Tuner/*.mdb
	rm -f Mono.Tuner/*.pdb

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

LINKER_FLAGS = -d $(WORKING_DIR) -o $(OUTPUT_DIR) -l none -c link -a smcs -b true
LINKER_STEPS = \
	Mono.Tuner.PrintStatus,PrintStatus:OutputStep	\
	Mono.Tuner.RemoveSerialization,RemoveSerialization:OutputStep	\
	Mono.Tuner.CheckVisibility,CheckVisibility	\

tune: $(MCS_MASTER_INFOS)
	MONO_PATH="Mono.Tuner$(PLATFORM_PATH_SEPARATOR)$(topdir)/class/lib/default$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(LINKER) $(LINKER_FLAGS) $(LINKER_DESCS:%=-x %) $(LINKER_STEPS:%=-s %) $(MCS_MASTER_INFOS:%=-i %)

gacutil = $(topdir)/class/lib/net_1_1_bootstrap/gacutil.exe
GACUTIL = MONO_PATH="$(topdir)/class/lib/net_1_1_bootstrap$(PLATFORM_PATH_SEPARATOR)$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(gacutil)

end-install: inst-mscorlib.dll inst-smcs.exe gac-System gac-System.Xml.Core gac-System.Core

inst-%:
	$(INSTALL_LIB) $(OUTPUT_DIR)/$*.mdb $(mono_libdir)/mono/2.1/$*.mdb
	test ! -f $(OUTPUT_DIR)/$*.mdb || $(INSTALL_LIB) $(OUTPUT_DIR)/$*.mdb $(mono_libdir)/mono/2.1/$*.mdb

gac-%:
	$(GACUTIL) /i $(OUTPUT_DIR)/$*.dll /f /root $(mono_libdir) /package 2.1

%.dll: %.cs
	$(CSCOMPILE) /t:library $^
