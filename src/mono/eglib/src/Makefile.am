noinst_LTLIBRARIES = libeglib.la

win_files  = \
	gdate-win32.c gdir-win32.c gfile-win32.c gmisc-win32.c \
	gmodule-win32.c gtimer-win32.c

unix_files = \
	gdate-unix.c  gdir-unix.c  gfile-unix.c  gmisc-unix.c	\
	gmodule-unix.c gtimer-unix.c

if PLATFORM_WIN32
os_files = $(win_files)
else
os_files = $(unix_files)
endif

libeglib_la_SOURCES = \
	eglib-config.h	\
	sort.frag.h	\
	glib.h	     	\
	garray.c	\
	gerror.c	\
	ghashtable.c 	\
	gmem.c       	\
	gmodule.h	\
	goutput.c    	\
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
	$(os_files)

libeglib_la_CFLAGS = -Wall -D_FORTIFY_SOURCE=2

INCLUDES = -I$(srcdir)

if PLATFORM_WIN32
libeglib_la_LIBADD = -lm -liconv -lpsapi
else
libeglib_la_LIBADD = -lm
endif

MAINTAINERCLEANFILES = Makefile.in

EXTRA_DIST = eglib-config.h.in $(win_files) $(unix_files)
