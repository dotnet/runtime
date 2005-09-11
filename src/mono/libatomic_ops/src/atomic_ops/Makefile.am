SUBDIRS=sysdeps

EXTRA_DIST=generalize-small.template

#Private Headers
private_HEADERS=generalize.h generalize-small.h
privatedir=${includedir}/atomic_ops/

generalize-small.h: generalize-small.template
	sed -e s:XSIZE:byte:g -e s:XCTYPE:char:g $? > $@
	sed -e s:XSIZE:short:g -e s:XCTYPE:short:g $? >> $@
	sed -e s:XSIZE:int:g -e s:XCTYPE:int:g $? >> $@
