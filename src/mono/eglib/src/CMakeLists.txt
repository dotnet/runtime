set(win_files
	gdate-win32.c gdir-win32.c gfile-win32.c gmisc-win32.c
	gmodule-win32.c gtimer-win32.c)

set(unix_files
	gdate-unix.c  gdir-unix.c  gfile-unix.c  gmisc-unix.c
	gmodule-unix.c gtimer-unix.c)

if (MSVC)
  set(os_files ${win_files})
else(MSVC)
  set(os_files ${unix_files})
endif(MSVC)

set(libeglib_la_SOURCES
	${os_files}
	eglib-config.h
	sort.frag.h
	glib.h
	garray.c
	gerror.c
	ghashtable.c
	gmem.c
	gmodule.h
	goutput.c
	gstr.c
	gslist.c
	gstring.c
	gptrarray.c
	glist.c
	gqueue.c
	gpath.c
	gshell.c
	gspawn.c
	gfile.c	
	gfile-posix.c
	gpattern.c
	gmarkup.c
	gutf8.c
	gunicode.c
	unicode-data.h)

set(EXTRA_DIST eglib-config.h.in eglib-config.hw ${win_files} ${unix_files})

INCLUDE_DIRECTORIES(../ ${CMAKE_CURRENT_SOURCE_DIR})
ADD_DEFINITIONS(${CFLAGS} -D_FORTIFY_SOURCE=2)
ADD_LIBRARY(eglib-static STATIC ${libeglib_la_SOURCES})
ADD_LIBRARY(eglib SHARED ${libeglib_la_SOURCES})

