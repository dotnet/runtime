thisdir = tools/tuner
SUBDIRS =
DEP_DIRS = linker
include ../../build/rules.make

LOCAL_MCS_FLAGS = -r:$(topdir)/class/lib/$(PROFILE)/monolinker.exe
LIB_REFS = System System.Core System.Xml Mono.Cecil

LIBRARY = Mono.Tuner.dll

NO_INSTALL = yes
NO_TEST = yes
NO_SIGN_ASSEMBLY = yes

include ../../build/library.make
