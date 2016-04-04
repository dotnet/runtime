thisdir = tools/linker
SUBDIRS =
include ../../build/rules.make

RESOURCES = \
	Descriptors/mscorlib.xml	\
	Descriptors/System.xml		\
	Descriptors/System.Core.xml		\
	Descriptors/System.Drawing.xml	\
	Descriptors/System.Web.xml		\
	Descriptors/Mono.Posix.xml		\

PROGRAM = monolinker.exe

$(PROGRAM): $(RESOURCES)

LIB_REFS = System System.Core System.Xml Mono.Cecil
LOCAL_MCS_FLAGS = $(RESOURCES:%=-resource:%)

EXTRA_DISTFILES = $(RESOURCES)

include ../../build/executable.make
