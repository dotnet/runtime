
AM_CPPFLAGS = $(GLIB_CFLAGS) -I$(top_srcdir)

noinst_LTLIBRARIES = libmonoarch-arm.la

BUILT_SOURCES = arm_dpimacros.h arm_fpamacros.h arm_vfpmacros.h


libmonoarch_arm_la_SOURCES = $(BUILT_SOURCES) \
	arm-codegen.c \
	arm-codegen.h \
	arm-dis.c \
	arm-dis.h

arm_dpimacros.h: dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th
	(cd $(srcdir); bash ./dpiops.sh) > $@t
	mv $@t $@

arm_fpamacros.h: fpaops.sh fpam_macros.th fpa_macros.th
	(cd $(srcdir); bash ./fpaops.sh) > $@t
	mv $@t $@

arm_vfpmacros.h: vfpops.sh vfpm_macros.th vfp_macros.th
	(cd $(srcdir); bash ./vfpops.sh) > $@t
	mv $@t $@

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th \
	fpam_macros.th fpa_macros.th arm-fpa-codegen.h fpaops.sh \
	vfpm_macros.th vfp_macros.th arm-vfp-codegen.h vfpops.sh

