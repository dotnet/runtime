
noinst_PROGRAMS = hps scratch shmdel

INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

hps_LDADD = \
	../io-layer/libwapi.a		\
	../utils/libmonoutils.a		\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

scratch_LDADD = \
	../io-layer/libwapi.a		\
	../utils/libmonoutils.a		\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

shmdel_LDADD = \
	../io-layer/libwapi.a		\
	../utils/libmonoutils.a		\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

EXTRA_DIST = ChangeLog

