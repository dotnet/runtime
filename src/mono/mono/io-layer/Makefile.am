lib_LIBRARIES = libwapi.a

INCLUDES = \
	$(GLIB_CFLAGS)		\
	$(GMODULE_CFLAGS)	\
	-D_WAPI_BUILDING=1	\
	-DMONO_BINDIR=\""$(bindir)"\"	\
	-I$(top_srcdir)	

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
	processes.h	\
	semaphores.h	\
	sockets.h	\
	status.h	\
	system.h	\
	threads.h	\
	timefuncs.h	\
	types.h		\
	uglify.h	\
	wait.h		\
	wapi.h	

OTHER_SRC = \
	atomic.c		\
	atomic.h		\
	context.c		\
	context.h		\
	critical-sections.c	\
	critical-sections.h	\
	daemon-messages.c	\
	daemon-messages.h	\
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
	io-private.h		\
	io-layer.h		\
	macros.h		\
	misc.c			\
	misc-private.h		\
	mutexes.c		\
	mutexes.h		\
	mutex-private.h		\
	mono-mutex.c		\
	mono-mutex.h		\
	processes.c		\
	processes.h		\
	process-private.h	\
	semaphores.c		\
	semaphores.h		\
	semaphore-private.h	\
	shared.c		\
	shared.h		\
	sockets.c		\
	sockets.h		\
	socket-private.h	\
	status.h		\
	system.c		\
	system.h		\
	threads.c		\
	threads.h		\
	thread-private.h	\
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
	wapi-private.h

WINDOWS_H = \
	io-layer.h

WINDOWS_SRC = \
	io-layer.h		\
	io-layer-dummy.c

if PLATFORM_WIN32
libwapi_a_SOURCES = $(WINDOWS_SRC)
libwapiinclude_HEADERS = $(WINDOWS_H)
else
libwapi_a_SOURCES = $(OTHER_SRC)
libwapiinclude_HEADERS = $(OTHER_H)

bin_PROGRAMS = mono-handle-d

mono_handle_d_SOURCES = \
	daemon.c	\
	daemon-messages.c	\
	daemon-messages.h

mono_handle_d_LDADD = \
	$(GLIB_LIBS) \
	$(top_builddir)/mono/io-layer/libwapi.a \
	$(top_builddir)/mono/utils/libmonoutils.a
endif

EXTRA_DIST =	\
	$(WINDOWS_SRC)	\
	$(OTHER_SRC)


