lib_LTLIBRARIES = libmonowrapper.la

INCLUDES = $(GLIB_CFLAGS) -I$(top_srcdir)

libmonowrapper_la_SOURCES = 		\
	glob.c				\
	wrapper.c	

libmonowrapperincludedir = $(includedir)/mono/wrapper

libmonowrapperinclude_HEADERS = 
	wrapper.h	

noinst_HEADERS = 

libmonowrapper.def: genwrapper.pl
	$(srcdir)/genwrapper.pl -d >libmonowrapper.def

Wrapper.cs: genwrapper.pl
	$(srcdir)/genwrapper.pl -c >Wrapper.cs

libmonowrapper.dll: $(libmonowrapper_la_OBJECTS) libmonowrapper.rc libmonowrapper.def
	./build-dll libmonowrapper 0.1


wrapper.h: genwrapper.pl
	$(srcdir)/genwrapper.pl >wrapper.h

BUILT_SOURCES = wrapper.h Wrapper.cs libmonowrapper.def

CLEANFILES = $(BUILT_SOURCES) lib*.a lib*.dll

EXTRA_DIST = genwrapper.pl

