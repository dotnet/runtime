noinst_LTLIBRARIES = libwapi.la

INCLUDES = \
	$(GLIB_CFLAGS)		\
	$(LIBGC_CFLAGS)		\
	-DMONO_BINDIR=\""$(bindir)"\"	\
	-I$(top_srcdir)	

libwapiincludedir = $(includedir)/mono-$(API_VER)/mono/io-layer

OTHER_H = \
	access.h	\
	atomic.h	\
	collection.h	\
	context.h	\
	critical-sections.h	\
	error.h		\
	events.h	\
	handles.h	\
	io.h		\
	io-layer.h	\
	io-portability.h	\
	macros.h	\
	messages.h	\
	mono-mutex.h	\
	mutexes.h	\
	processes.h	\
	security.h	\
	semaphores.h	\
	sockets.h	\
	status.h	\
	system.h	\
	threads.h	\
	timefuncs.h	\
	types.h		\
	uglify.h	\
	versioninfo.h	\
	wait.h		\
	wapi.h	

OTHER_SRC = \
	access.h		\
	atomic.c		\
	atomic.h		\
	collection.c		\
	collection.h		\
	context.c		\
	context.h		\
	critical-sections.c	\
	critical-sections.h	\
	critical-section-private.h	\
	error.c			\
	error.h			\
	events.c		\
	events.h		\
	event-private.h		\
	handles.c		\
	handles.h		\
	handles-private.h	\
	io.c			\
	io.h			\
	io-portability.c	\
	io-portability.h	\
	io-private.h		\
	io-layer.h		\
	macros.h		\
	messages.c		\
	messages.h		\
	misc.c			\
	misc-private.h		\
	mutexes.c		\
	mutexes.h		\
	mutex-private.h		\
	mono-mutex.c		\
	mono-mutex.h		\
	mono-spinlock.h		\
	processes.c		\
	processes.h		\
	process-private.h	\
	security.c		\
	security.h		\
	semaphores.c		\
	semaphores.h		\
	semaphore-private.h	\
	shared.c		\
	shared.h		\
	sockets.c		\
	sockets.h		\
	socket-private.h	\
	socket-wrappers.h	\
	status.h		\
	system.c		\
	system.h		\
	threads.c		\
	threads.h		\
	thread-private.h	\
	timefuncs.c		\
	timefuncs.h		\
	timefuncs-private.h	\
	types.h			\
	uglify.h		\
	versioninfo.c		\
	versioninfo.h		\
	wait.c			\
	wait.h			\
	wapi_glob.h		\
	wapi_glob.c		\
	wapi.h			\
	wapi-private.h

WINDOWS_H = \
	io-layer.h

WINDOWS_SRC = \
	io-layer.h		\
	io-layer-dummy.c

HPPA_SRC = \
	hppa_atomic.S

if PLATFORM_WIN32
libwapi_la_SOURCES = $(WINDOWS_SRC)
libwapiinclude_HEADERS = $(WINDOWS_H)
else
if HPPA
libwapi_la_SOURCES = $(OTHER_SRC) $(HPPA_SRC)
# to enable pick up of config.h
libwapi_la_CCASFLAGS = -I$(top_builddir)
else
libwapi_la_SOURCES = $(OTHER_SRC)
endif
libwapiinclude_HEADERS = $(OTHER_H)
endif

EXTRA_DIST =	\
	$(WINDOWS_SRC)	\
        $(HPPA_SRC) \
	$(OTHER_SRC)


