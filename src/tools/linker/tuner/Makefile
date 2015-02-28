thisdir = tools/tuner
SUBDIRS =
DEP_DIRS = linker
include ../../build/rules.make

CECIL = $(topdir)/class/lib/$(PROFILE)/Mono.Cecil.dll
LINKER = $(topdir)/class/lib/$(PROFILE)/monolinker.exe

LOCAL_MCS_FLAGS = -r:System.dll -r:System.Core.dll -r:System.Xml.dll -r:$(LINKER) -r:$(CECIL)

LIBRARY = Mono.Tuner.dll

NO_INSTALL = yes
NO_TEST = yes
NO_SIGN_ASSEMBLY = yes

include ../../build/library.make
