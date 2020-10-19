MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

if !ENABLE_MSVC_ONLY

SUBDIRS = . test

noinst_LTLIBRARIES = libeglib.la

AM_CFLAGS = $(SHARED_CFLAGS)

win_files  = \
	eglib-config.hw \
	gdate-win32.c gdir-win32.c gfile-win32.c gmisc-win32.c \
	gmodule-win32.c gtimer-win32.c gunicode-win32.c

unix_files = \
	gdate-unix.c  gdir-unix.c  gfile-unix.c  gmisc-unix.c	\
	gmodule-unix.c gtimer-unix.c gmodule-aix.c \
	gspawn.c

if HOST_WIN32
os_files = $(win_files)
else
os_files = $(unix_files)
endif

libeglib_la_SOURCES = \
	eglib-remap.h	\
	sort.frag.h	\
	glib.h	     	\
	garray.c	\
	gbytearray.c	\
	gerror.c	\
	ghashtable.c 	\
	giconv.c	\
	gmem.c       	\
	gmodule.h	\
	goutput.c    	\
	gqsort.c	\
	gstr.c       	\
	gslist.c     	\
	gstring.c    	\
	gptrarray.c     \
	glist.c		\
	gqueue.c	\
	gpath.c		\
	gshell.c	\
	gfile.c		\
	gfile-posix.c	\
	gpattern.c	\
	gmarkup.c	\
	gutf8.c		\
	gunicode.c	\
	unicode-data.h	\
	$(os_files)

endif # !ENABLE_MSVC_ONLY

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@) @CXX_ADD_CFLAGS@

if !ENABLE_MSVC_ONLY

eglibdir=$(datadir)/mono-$(API_VER)/mono/eglib
eglib_DATA = eglib-config.h

libeglib_la_CFLAGS = -g -Wall -D_GNU_SOURCE

if !HOST_WIN32
libeglib_la_CFLAGS += -D_FORTIFY_SOURCE=2
endif

AM_CPPFLAGS = -I$(srcdir)

if HOST_ANDROID
libeglib_la_LIBADD = -lm -llog
else
if HOST_WIN32
libeglib_la_LIBADD = -lm -lpsapi $(LTLIBICONV)
else
libeglib_la_LIBADD = -lm $(LTLIBICONV)
endif
endif

MAINTAINERCLEANFILES = Makefile.in

EXTRA_DIST = eglib-config.h.in $(win_files) $(unix_files)

endif # !ENABLE_MSVC_ONLY

if ENABLE_MSVC_ONLY

all-local:

	make -C $(top_srcdir)/msvc eglib

clean-local:

	make -C $(top_srcdir)/msvc clean-eglib


endif # ENABLE_MSVC_ONLY
