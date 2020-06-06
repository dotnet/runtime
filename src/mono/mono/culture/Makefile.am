MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(GLIB_CFLAGS) $(SHARED_CFLAGS)

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@)

if !ENABLE_MSVC_ONLY

noinst_LTLIBRARIES = libmono-culture.la

if !HOST_WIN32
unix_sources = w32process-unix-language.c

platform_sources = $(unix_sources)
endif

libmono_culture_la_SOURCES = \
	$(platform_sources)	\
	culture-info.h	\
	culture-info-tables.h	\
	locales.c
libmono_culture_la_CPPFLAGS = $(AM_CPPFLAGS) @CXX_ADD_CFLAGS@

endif # ENABLE_MSVC_ONLY
