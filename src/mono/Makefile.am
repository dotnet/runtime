AUTOMAKE_OPTIONS = foreign

SUBDIRS = @libgc_dir@ mono doc docs runtime scripts man data

dnl EXTRA_SUBDIRS = libgc

EXTRA_DIST= mono.pc.in mono.spec.in

pkgconfigdir = $(libdir)/pkgconfig
pkgconfig_DATA= mono.pc
DISTCLEANFILES= mono.pc

fullbuild: remove-binaries mcs-tree-safe-build xinstall-runtime mcs-rest install

mcs-tree-safe-build:
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