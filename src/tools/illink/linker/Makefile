thisdir = tools/linker
SUBDIRS =
include ../../build/rules.make

PROGRAM_SNK = ../../class/mono.snk

CECIL = $(topdir)/class/lib/net_1_1/Mono.Cecil.dll

RESOURCES = \
	Descriptors/mscorlib.xml	\
	Descriptors/System.xml		\
	Descriptors/System.Drawing.xml	\
	Descriptors/System.Web.xml		\
	Descriptors/Mono.Posix.xml		\

PROGRAM = $(topdir)/class/lib/$(PROFILE)/monolinker.exe

$(PROGRAM): $(RESOURCES)

LOCAL_MCS_FLAGS = /r:$(CECIL) /r:System.Xml.dll -keyfile:$(PROGRAM_SNK) $(RESOURCES:%=-resource:%)

EXTRA_DISTFILES = $(RESOURCES)

include ../../build/executable.make
