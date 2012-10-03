noinst_LTLIBRARIES = libeglib.la libeglib-static.la

win_files  = \
	eglib-config.hw \
	gdate-win32.c gdir-win32.c gfile-win32.c gmisc-win32.c \
	gmodule-win32.c gtimer-win32.c

unix_files = \
	gdate-unix.c  gdir-unix.c  gfile-unix.c  gmisc-unix.c	\
	gmodule-unix.c gtimer-unix.c

# some unices and windows do not have an implementation of vasprintf
# used by eglib, use provided implementation instead
if NEED_VASPRINTF
vasprintf_files = vasprintf.c
else
vaprinttf_files = foo.c
endif

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
	gspawn.c	\
	gfile.c		\
	gfile-posix.c	\
	gpattern.c	\
	gmarkup.c	\
	gutf8.c		\
	gunicode.c	\
	unicode-data.h	\
	$(os_files)	\
	$(vasprintf_files)

libeglib_la_CFLAGS = -g -Wall -D_FORTIFY_SOURCE=2
libeglib_static_la_SOURCES=$(libeglib_la_SOURCES)
libeglib_static_la_CFLAGS = $(libeglib_la_CFLAGS)

AM_CPPFLAGS = -I$(srcdir)

if HOST_WIN32
libeglib_la_LIBADD = -lm $(LIBICONV) -lpsapi
else
if PLATFORM_ANDROID
libeglib_la_LIBADD = -llog
endif
endif

libeglib_static_la_LIBADD = $(libeglib_la_LIBADD) $(LIBICONV)
libeglib_static_la_LDFLAGS = -static

MAINTAINERCLEANFILES = Makefile.in

EXTRA_DIST = eglib-config.h.in $(win_files) $(unix_files)
