
INCLUDES = \
	-fexceptions -DMONO_USE_EXC_TABLES	\
	-I$(top_srcdir) 	\
	$(GMODULE_CFLAGS)		\
	$(GLIB_CFLAGS)

if JIT_SUPPORTED

lib_LTLIBRARIES = libmono-profiler-cov.la
endif

libmono_profiler_cov_la_SOURCES = mono-cov.c
libmono_profiler_cov_la_LIBADD = $(top_builddir)/mono/mini/libmono.la

