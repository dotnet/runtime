lib_LIBRARIES = libwapi.a

INCLUDES = \
	$(GLIB_CFLAGS)		\
	$(GMODULE_CFLAGS)	\
	-I$(top_srcdir)		\
	#

libwapiincludedir = $(includedir)/mono/io-layer

OTHER_H = \
	critical-sections.h	\
	error.h		\
	handles.h	\
	io.h		\
	io-layer.h	\
	mutexes.h	\
	semaphores.h	\
	status.h	\
	threads.h	\
	uglify.h	\
	wait.h		\
	wapi.h		\
	#

OTHER_SRC = \
	critical-sections.c	\
	critical-sections.h	\
	error.c			\
	error.h			\
	handles.c		\
	handles.h		\
	handles-private.h	\
	io.c			\
	io.h			\
	io-layer.h		\
	misc.c			\
	misc-private.h		\
	mutexes.c		\
	mutexes.h		\
	pthread-compat.c	\
	pthread-compat.h	\
	semaphores.c		\
	semaphores.h		\
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


