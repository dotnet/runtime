ACLOCAL_AMFLAGS = -I m4

AM_CFLAGS = $(WERROR_CFLAGS)

if HOST_WIN32
SUBDIRS = m4 src
else
if PLATFORM_DARWIN
SUBDIRS = m4 src
else
if CROSS_COMPILING
SUBDIRS = m4 src
else
SUBDIRS = m4 src test
endif
endif
endif

EXTRA_DIST = autogen.sh README NEWS AUTHORS winconfig.h
