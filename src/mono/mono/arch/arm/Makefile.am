
INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

noinst_LTLIBRARIES = libmonoarch-arm.la

BUILT_SOURCES = arm_dpimacros.h


libmonoarch_arm_la_SOURCES = $(BUILT_SOURCES) \
	tramp.c \
	arm-codegen.c \
	arm-codegen.h \
	arm-dis.c \
	arm-dis.h \
	#

arm_dpimacros.h: dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th
	bash ./dpiops.sh

CLEANFILES = $(BUILT_SOURCES)

EXTRA_DIST = dpiops.sh mov_macros.th dpi_macros.th cmp_macros.th

