noinst_PROGRAMS = test-eglib test-glib

SOURCES = \
	test.c 		\
	test.h 		\
	tests.h 	\
	driver.c 	\
	hashtable.c 	\
	string-util.c 	\
	string.c 	\
	slist.c 	\
	sizes.c		\
	ptrarray.c 	\
	list.c

test_eglib_SOURCES = $(SOURCES)
test_glib_SOURCES = $(SOURCES)

test_eglib_CFLAGS = -Wall -Werror -D_FORTIFY_SOURCE=2 -I$(srcdir)/../src -DDRIVER_NAME=\"EGlib\"
test_eglib_LDADD = -L../src -leglib

test_glib_CFLAGS = `pkg-config --cflags glib-2.0` -DDRIVER_NAME=\"GLib\"
test_glib_LDFLAGS = `pkg-config --libs glib-2.0`

MAINTAINERCLEANFILES = Makefile.in

