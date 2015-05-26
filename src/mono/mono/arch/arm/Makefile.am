
AM_CPPFLAGS = $(GLIB_CFLAGS) -I$(top_srcdir)

BUILT_SOURCES = arm_dpimacros.h arm_vfpmacros.h

arm_dpimacros.h: dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th
	(cd $(srcdir); bash ./dpiops.sh) > $@t
	mv $@t $@

arm_vfpmacros.h: vfpops.sh vfpm_macros.th vfp_macros.th
	(cd $(srcdir); bash ./vfpops.sh) > $@t
	mv $@t $@

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = arm-codegen.h dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th \
	vfpm_macros.th vfp_macros.th arm-vfp-codegen.h vfpops.sh

