lib_LIBRARIES = libwapi.a

INCLUDES = \
	$(GLIB_CFLAGS)		\
	$(GMODULE_CFLAGS)	\
	-D_WAPI_BUILDING=1	\
	-I$(top_srcdir)		\
	#

libwapiincludedir = $(includedir)/mono/io-layer

OTHER_H = \
	atomic.h	\
	context.h	\
	critical-sections.h	\
	error.h		\
	events.h	\
	handles.h	\
	io.h		\
	io-layer.h	\
	macros.h	\
	mutexes.h	\
	semaphores.h	\
	sockets.h	\
	status.h	\
	system.h	\
	threads.h	\
	timefuncs.h	\
	types.h		\
	uglify.h	\
	wait.h		\
	wapi.h		\
	#

OTHER_SRC = \
	atomic.c		\
	atomic.h		\
	context.c		\
	context.h		\
	critical-sections.c	\
	critical-sections.h	\
	error.c			\
	error.h			\
	events.c		\
	events.h		\
	handles.c		\
	handles.h		\
	handles-private.h	\
	io.c			\
	io.h			\
	io-layer.h		\
	macros.h		\
	misc.c			\
	misc-private.h		\
	mutexes.c		\
	mutexes.h		\
	mono-mutex.c		\
	mono-mutex.h		\
	semaphores.c		\
	semaphores.h		\
	sockets.c		\
	sockets.h		\
	status.h		\
	system.c		\
	system.h		\
	threads.c		\
	threads.h		\
	timefuncs.c		\
	timefuncs.h		\
	timed-thread.c		\
	timed-thread.h		\
	types.h			\
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

WINDOWS_SRC = \
	io-layer.h		\
	io-layer-dummy.c	\
	#

if PLATFORM_WIN32
libwapi_a_SOURCES = $(WINDOWS_SRC)
libwapiinclude_HEADERS = $(WINDOWS_H)
else
libwapi_a_SOURCES = $(OTHER_SRC)
libwapiinclude_HEADERS = $(OTHER_H)
endif

EXTRA_DIST =	\
	$(WINDOWS_SRC)	\
	$(OTHER_SRC)	\
	#


