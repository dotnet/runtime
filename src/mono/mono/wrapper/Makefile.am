lib_LTLIBRARIES = libmonowrapper.la

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

libmonowrapper_la_SOURCES = 		\
	wrapper.c	

libmonowrapperincludedir = $(includir)/mono/wrapper

libmonowrapperinclude_HEADERS = 
	wrapper.h	

noinst_HEADERS =


wrapper.h: genwrapper.pl
	$(srcdir)/genwrapper.pl >wrapper.h

BUILT_SOURCES = wrapper.h
CLEANFILES += $(BUILT_SOURCES)

EXTRA_DIST = genwrapper.pl