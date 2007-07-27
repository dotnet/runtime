thisdir = tools/tuner
SUBDIRS =
include ../../build/rules.make

LINKER = ../linker/monolinker.exe
MASTER_INFOS_ZIP = http://mono.ximian.com/masterinfos/masterinfo-sl11a.zip

LINKER_PLUGINS = \
	Mono.Tuner/PrintStatus.dll	\
	Mono.Tuner/RemoveSerialization.dll	\
#	Mono.Tuner/InjectAttributes.dll	\

LOCAL_MCS_FLAGS = -r:$(LINKER) -r:Mono.Cecil.dll

LINKER_SOURCES = $(LINKER_PLUGINS:.dll=.cs)

DISTFILES = README $(LINKER_PLUGINS)

ifneq (net_2_1, $(PROFILE))
all-local: $(LINKER_PLUGINS)
else
all-local:
endif

ifeq (net_2_1, $(PROFILE))
install-local: tune
else
install-local:
endif

uninstall-local:

clean-local:
	rm -f Mono.Tuner/*.dll
	rm -f Mono.Tuner/*.mdb
	rm -f Mono.Tuner/*.pdb
	rm -rf masterinfos*

dist-local: dist-default

test-local:

run-test-local run-test-ondotnet-local:

WORKING_DIR = $(mono_libdir)/mono/2.1

masterinfos/silverlight.infos:
	wget -O masterinfos.zip $(MASTER_INFOS_ZIP)
	unzip masterinfos.zip

MCS_MASTER_INFOS =	\
	masterinfos/silverlight/mscorlib.info	\
	masterinfos/silverlight/System.info		\
	masterinfos/silverlight/System.Core.info	\
	masterinfos/silverlight/System.Xml.Core.info	\

LINKER_FLAGS = -d $(WORKING_DIR) -o $(WORKING_DIR) -l none -c link -a smcs
#LINKER_STEPS = -s Mono.Tuner.PrintStatus,PrintStatus -s Mono.Tuner.InjectAttributes,InjectAttributes:OutputStep
LINKER_STEPS = -s Mono.Tuner.PrintStatus,PrintStatus:OutputStep -s Mono.Tuner.RemoveSerialization,RemoveSerialization:OutputStep

tune: masterinfos/silverlight.infos
	MONO_PATH="Mono.Tuner:$$MONO_PATH" $(RUNTIME) $(RUNTIME_FLAGS) $(LINKER) $(LINKER_FLAGS) $(LINKER_STEPS) $(MCS_MASTER_INFOS:%=-i %)

%.dll: %.cs
	$(CSCOMPILE) /t:library $^
