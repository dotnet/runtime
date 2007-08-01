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

LOCAL_MCS_FLAGS = -r:$(LINKER) -r:Mono.Cecil.dll

LINKER_SOURCES = $(LINKER_PLUGINS:.dll=.cs)

DISTFILES = README $(LINKER_SOURCES)

ifneq (net_2_1, $(PROFILE))
all-local: $(LINKER_PLUGINS)

install-local:

clean-local:
	rm -f Mono.Tuner/*.dll
	rm -f Mono.Tuner/*.mdb
	rm -f Mono.Tuner/*.pdb

else
all-local: tune

install-local:

clean-local:
	rm -rf masterinfos*
endif

uninstall-local:

dist-local: dist-default

test-local:

run-test-local run-test-ondotnet-local:

WORKING_DIR = $(topdir)/class/lib/net_2_1

masterinfos/silverlight.infos:
	wget -O masterinfos.zip $(MASTER_INFOS_ZIP)
	unzip masterinfos.zip

MCS_MASTER_INFOS =	\
	masterinfos/silverlight/mscorlib.info	\
	masterinfos/silverlight/System.info		\
	masterinfos/silverlight/System.Core.info	\
	masterinfos/silverlight/System.Xml.Core.info	\

LINKER_FLAGS = -d $(WORKING_DIR) -o $(WORKING_DIR) -l none -c link -a smcs
LINKER_STEPS = \
	Mono.Tuner.PrintStatus,PrintStatus:OutputStep	\
	Mono.Tuner.RemoveSerialization,RemoveSerialization:OutputStep	\
	Mono.Tuner.CheckVisibility,CheckVisibility	\

tune: masterinfos/silverlight.infos
	MONO_PATH="Mono.Tuner:$(topdir)/class/lib/net_2_0:$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(LINKER) $(LINKER_FLAGS) $(LINKER_STEPS:%=-s %) $(MCS_MASTER_INFOS:%=-i %)

%.dll: %.cs
	$(CSCOMPILE) /t:library $^
