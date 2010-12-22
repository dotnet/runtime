if HOST_WIN32
SUBDIRS = src
else
if PLATFORM_DARWIN
SUBDIRS = src
else
if CROSS_COMPILING
SUBDIRS = src
else
SUBDIRS = src test
endif
endif
endif

EXTRA_DIST = autogen.sh README NEWS AUTHORS ChangeLog winconfig.h

