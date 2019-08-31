include $(top_srcdir)/mk/common.mk

EXTRA_DIST = UTF-8.txt UTF-16BE.txt UTF-16LE.txt UTF-32BE.txt UTF-32LE.txt

SOURCES = \
	enum.cpp	\
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

GLIB_TEST_FLAGS_COMMON = -Wall -DEGLIB_TESTS=1 -D_FORTIFY_SOURCE=2 -I$(srcdir)/.. -I.. -DDRIVER_NAME=\"EGlib\"
CFLAGS += $(GLIB_TEST_FLAGS_COMMON)
CXXFLAGS += $(GLIB_TEST_FLAGS_COMMON) @CXXFLAGS_COMMON@

test_eglib_LDADD = ../libeglib.la $(LTLIBICONV)
assertf_LDADD = ../libeglib.la $(LTLIBICONV)

abs_srcdir = $(abspath $(srcdir))

if DISABLE_EXECUTABLES
run-eglib:
else
# FIXME: Various unit tests are broken on Win32, see https://github.com/mono/mono/issues/16576
if HOST_WIN32
run-eglib:
else
run-eglib:
	srcdir="$(abs_srcdir)" ./test-eglib

noinst_PROGRAMS = test-eglib assertf
endif # !HOST_WIN32
endif # DISABLE_EXECUTABLES

run-both: run-eglib

check-local: run-both

MAINTAINERCLEANFILES = Makefile.in
