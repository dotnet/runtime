DIST_SUBDIRS = unix win32

INCLUDES = -I$(top_srcdir)

noinst_LTLIBRARIES = libmonoos.la

if PLATFORM_WIN32
DIR=win32
else
DIR=unix
endif

SUBDIRS = $(DIR)

libmonoos_la_SOURCES = \
	util.h \
	gc_wrapper.h

libmonoos_la_LIBADD = $(DIR)/libmonoos.la

