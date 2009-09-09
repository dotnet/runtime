thisdir = tools/tuner
SUBDIRS =
include ../../build/rules.make

CECIL = $(topdir)/class/lib/net_1_1/Mono.Cecil.dll
LINKER = $(topdir)/class/lib/net_2_0/monolinker.exe

LOCAL_MCS_FLAGS = -r:System.dll -r:System.Xml.dll -r:$(LINKER) -r:$(CECIL)

LIBRARY = Mono.Tuner.dll

NO_INSTALL = yes
NO_TEST = yes
NO_SIGN_ASSEMBLY = yes

include ../../build/library.make
