if PLATFORM_WIN32
# Use -m here. This will use / as directory separator (C:/WINNT).
# The files that use MONO_ASSEMBLIES and/or MONO_CFG_DIR replace the
# / by \ if running under WIN32.
assembliesdir = `cygpath -m "${libdir}"`
confdir = `cygpath -m "${sysconfdir}"`
else
assembliesdir = $(exec_prefix)/lib
confdir = $(sysconfdir)
endif

INCLUDES = -I$(top_srcdir) -DMONO_ASSEMBLIES=\"$(assembliesdir)\" -DMONO_CFG_DIR=\"$(confdir)\" $(GLIB_CFLAGS) 

noinst_LTLIBRARIES = libmonoos.la

libmonoos_la_SOURCES = util.c
