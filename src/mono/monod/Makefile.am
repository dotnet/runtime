if PLATFORM_WIN32
bin_PROGRAMS = 
else
bin_PROGRAMS = monod
endif

if JIT_SUPPORTED
runtime_lib=../mono/mini/libmono.la
else
runtime_lib=../mono/interpreter/libmint.la
endif

monod_SOURCES = monod.c

INCLUDES = 				\
	-I$(top_srcdir)			\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

monod_LDADD = \
	$(runtime_lib)			\
	$(GLIB_LIBS)			\
	$(GMODULE_LIBS) 		\
	-lm

EXTRA_DIST = ChangeLog
