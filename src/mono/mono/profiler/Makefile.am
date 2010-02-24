
INCLUDES = \
	-fexceptions -DMONO_USE_EXC_TABLES	\
	-I$(top_srcdir) 	\
	$(GLIB_CFLAGS)

if !DISABLE_PROFILER
if JIT_SUPPORTED
if PLATFORM_LINUX
lib_LTLIBRARIES = libmono-profiler-cov.la libmono-profiler-aot.la libmono-profiler-logging.la libmono-profiler-iomap.la
else
lib_LTLIBRARIES = libmono-profiler-cov.la libmono-profiler-aot.la libmono-profiler-iomap.la
endif
endif
endif

if HAVE_OPROFILE
# Do something that uses OPROFILE_CFLAGS and OPROFILE_LIBS
endif

libmono_profiler_cov_la_SOURCES = mono-cov.c
libmono_profiler_cov_la_LIBADD = $(top_builddir)/mono/mini/libmono-$(API_VER).la
libmono_profiler_aot_la_SOURCES = mono-profiler-aot.c
libmono_profiler_aot_la_LIBADD = $(top_builddir)/mono/mini/libmono-$(API_VER).la
libmono_profiler_logging_la_SOURCES = mono-profiler-logging.c
libmono_profiler_logging_la_LIBADD = $(top_builddir)/mono/mini/libmono-$(API_VER).la
libmono_profiler_iomap_la_SOURCES = mono-profiler-iomap.c
libmono_profiler_iomap_la_LIBADD = $(top_builddir)/mono/mini/libmono-$(API_VER).la
