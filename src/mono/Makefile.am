AUTOMAKE_OPTIONS = foreign

SUBDIRS = @libgc_dir@ mono doc docs runtime scripts man data

dnl EXTRA_SUBDIRS = libgc

EXTRA_DIST= mono.pc.in mono.spec.in

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA= mono.pc
DISTCLEANFILES= mono.pc

#
# The following target is used to compile and install
# mono and mcs without running into the out-of-sync
# problems by doing the compilation in the right order.
#

fullbuild: remove-binaries mcs-tree-safe-build xinstall-runtime mcs-rest install

mcs-tree-safe-build:
	(cd ../mcs/jay; make)
	(cd ../mcs/mcs; make MCS=mcs)
	(cd ../mcs/class/corlib; make MCS=mcs)
	cp ../mcs/class/lib/corlib.dll runtime
	cp ../mcs/mcs/mcs.exe runtime

xinstall-runtime:
	if echo $(SUBDIRS) | grep "libgc " > /dev/null; then (cd libgc; make && make install); fi
	(cd mono; make && make install)

mcs-rest:
	(cd ../mcs/class; make)

remove-binaries:
	rm ../mcs/class/lib/corlib.dll ../mcs/mcs/mcs.exe runtime/*dll runtime/*.exe >& /dev/null; echo