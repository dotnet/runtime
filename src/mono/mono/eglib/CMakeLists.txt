
set(eglib_win32_sources
    gdate-win32.c gdir-win32.c gfile-win32.c gmisc-win32.c
    gmodule-win32.c gtimer-win32.c gunicode-win32.c)

set(eglib_unix_sources
    gdate-unix.c  gdir-unix.c  gfile-unix.c  gmisc-unix.c
    gmodule-unix.c gtimer-unix.c)

if(HOST_WIN32)
set(eglib_platform_sources ${eglib_win32_sources})
else()
set(eglib_platform_sources ${eglib_unix_sources})
endif()

set(eglib_common_sources
    eglib-remap.h
    sort.frag.h
    glib.h
    garray.c
    gbytearray.c
    gerror.c
    ghashtable.c
    giconv.c
    gmem.c
    gmodule.h
    goutput.c
    gqsort.c
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

addprefix(eglib_sources ../eglib/ "${eglib_platform_sources};${eglib_common_sources}")

add_library(eglib_objects OBJECT "${eglib_sources}")
