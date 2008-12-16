thisdir = tools/tuner
SUBDIRS =
include ../../build/rules.make

LINKER = $(topdir)/class/lib/net_1_1/monolinker.exe

LOCAL_MCS_FLAGS = -r:System.Xml.dll -r:$(LINKER) -r:Mono.Cecil.dll

LIBRARY = Mono.Tuner.dll

NO_INSTALL = yes
NO_TEST = yes
NO_SIGN_ASSEMBLY = yes

include ../../build/library.make
