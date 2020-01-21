MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

AM_CPPFLAGS = $(GLIB_CFLAGS) -I$(top_srcdir)

arm_dpimacros.h: dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th
	(cd $(srcdir); bash ./dpiops.sh) > $@t
	mv $@t $@

arm_vfpmacros.h: vfpops.sh vfpm_macros.th vfp_macros.th
	(cd $(srcdir); bash ./vfpops.sh) > $@t
	mv $@t $@

gen: arm_dpimacros.h arm_vfpmacros.h

EXTRA_DIST = arm-codegen.h dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th \
	vfpm_macros.th vfp_macros.th arm-vfp-codegen.h vfpops.sh \
	arm_dpimacros.h arm_vfpmacros.h


