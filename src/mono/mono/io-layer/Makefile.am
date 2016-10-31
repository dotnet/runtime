
noinst_LTLIBRARIES = libwapi.la

AM_CPPFLAGS = \
	$(GLIB_CFLAGS)		\
	$(LIBGC_CPPFLAGS)	\
	-DMONO_BINDIR=\""$(bindir)"\"	\
	-I$(top_srcdir) \
	$(SHARED_CFLAGS)

libwapiincludedir = $(includedir)/mono-$(API_VER)/mono/io-layer

OTHER_H = \
	error.h		\
	io.h		\
	io-trace.h	\
	io-layer.h	\
	io-portability.h	\
	macros.h	\
	messages.h	\
	security.h	\
	sockets.h	\
	status.h	\
	timefuncs.h	\
	types.h		\
	uglify.h	\
	versioninfo.h	\
	wait.h		\
	wapi.h		\
	wapi-remap.h

OTHER_SRC = \
	error.c			\
	error.h			\
	io.c			\
	io.h			\
	io-portability.c	\
	io-portability.h	\
	io-private.h		\
	io-layer.h		\
	locking.c		\
	macros.h		\
	messages.c		\
	messages.h		\
	posix.c			\
	security.c		\
	security.h		\
	sockets.c		\
	sockets.h		\
	socket-private.h	\
	socket-wrappers.h	\
	status.h		\
	timefuncs.c		\
	timefuncs.h		\
	types.h			\
	uglify.h		\
	versioninfo.c		\
	versioninfo.h		\
	wait.c			\
	wait.h			\
	wapi_glob.h		\
	wapi_glob.c		\
	wapi.h			\
	wapi-private.h		\
	wapi.c


WINDOWS_H = \
	io-layer.h

WINDOWS_SRC = \
	io-layer.h		\
	io-layer-dummy.c

if HOST_WIN32
libwapi_la_SOURCES = $(WINDOWS_SRC) $(WINDOWS_H)
else
libwapi_la_SOURCES = $(OTHER_SRC) $(OTHER_H)
endif
if PLATFORM_DARWIN
libwapi_la_LIBADD = -lproc
endif

EXTRA_DIST =	\
	$(WINDOWS_SRC)
	$(OTHER_SRC)


