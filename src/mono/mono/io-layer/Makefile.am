lib_LIBRARIES = libwapi.a

INCLUDES = \
	$(GLIB_CFLAGS)		\
	$(GMODULE_CFLAGS)	\
	-I$(top_srcdir)		\
	#

libwapiincludedir = $(includedir)/mono/wapi

OTHER_H = \
	error.h		\
	handles.h	\
	io.h		\
	io-layer.h	\
	status.h	\
	threads.h	\
	uglify.h	\
	wait.h		\
	wapi.h		\
	#

OTHER_SOURCES = \
	error.c			\
	error.h			\
	handles.c		\
	handles.h		\
	handles-private.h	\
	io.c			\
	io.h			\
	io-layer.h		\
	pthread-compat.c	\
	pthread-compat.h	\
	status.h		\
	threads.c		\
	threads.h		\
	timed-thread.c		\
	timed-thread.h		\
	uglify.h		\
	unicode.c		\
	unicode.h		\
	wait.c			\
	wait.h			\
	wait-private.h		\
	wapi.h			\
	wapi-private.h		\
	#

WINDOWS_H = \
	io-layer.h		\
	#

WINDOWS_SOURCES = \
	io-layer.h		\
	io-layer-dummy.c	\
	#

if PLATFORM_WIN32
libwapi_a_SOURCES = $(WINDOWS_SOURCES)
libwapiinclude_HEADERS = $(WINDOWS_H)
else
libwapi_a_SOURCES = $(OTHER_SOURCES)
libwapiinclude_HEADERS = $(OTHER_H)
endif

EXTRA_DIST =	\
	$(WINDOWS_SOURCES)	\
	$(OTHER_SOURCES)	\
	#


