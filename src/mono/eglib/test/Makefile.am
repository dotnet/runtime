EXTRA_DIST = UTF-8.txt UTF-16BE.txt UTF-16LE.txt UTF-32BE.txt UTF-32LE.txt

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
	list.c		\
	array.c		\
	fake.c		\
	path.c		\
	queue.c		\
	shell.c		\
	spawn.c		\
	timer.c		\
	file.c		\
	pattern.c	\
	dir.c		\
	markup.c	\
	unicode.c	\
	utf8.c		\
	endian.c	\
	module.c	\
	memory.c

test_eglib_SOURCES = $(SOURCES)

test_eglib_CFLAGS = -Wall -DEGLIB_TESTS=1 -D_FORTIFY_SOURCE=2 -I$(srcdir)/../src -I../src -DDRIVER_NAME=\"EGlib\"
test_eglib_LDADD = ../src/libeglib.la $(LIBICONV)

run-eglib: all
	srcdir=`readlink -f $(srcdir)` ./test-eglib

if HAVE_GLIB

noinst_PROGRAMS = test-glib test-eglib

test_glib_SOURCES = $(SOURCES)

test_glib_CFLAGS = `pkg-config --cflags glib-2.0` -DDRIVER_NAME=\"GLib\"
test_glib_LDFLAGS = `pkg-config --libs glib-2.0 gmodule-2.0`

run-glib: all
	srcdir=`readlink -f $(srcdir)` ./test-glib

run-both: run-glib run-eglib

else

noinst_PROGRAMS = test-eglib

run-both: run-eglib

endif

MAINTAINERCLEANFILES = Makefile.in
