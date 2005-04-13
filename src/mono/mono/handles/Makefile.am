
noinst_PROGRAMS = hps

INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

hps_LDADD = \
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	$(LIBGC_LIBS)			\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

EXTRA_DIST = ChangeLog

