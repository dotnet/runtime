
noinst_PROGRAMS = hps semdel

INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GLIB_CFLAGS)

hps_LDADD = \
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	$(LIBGC_LIBS)			\
	$(GLIB_LIBS)			\
	-lm

semdel_LDADD = \
	../io-layer/libwapi.la		\
	../utils/libmonoutils.la	\
	$(LIBGC_LIBS)			\
	$(GLIB_LIBS)			\
	-lm

EXTRA_DIST = ChangeLog

