MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

# FIXME
TMPFLAGS=-D_AMD64_ -DBIT64 -D_ASSERTE= -DFEATURE_REDHAWK -DPLATFORM_UNIX -DHOST_64BIT -DHOST_AMD64 -DTARGET_UNIX


if SUPPORT_COREGC
shared_libraries = libmonocoregc.la
endif

noinst_LTLIBRARIES = $(shared_libraries)

coreclr_srcdir = $(top_srcdir)/../coreclr


AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono \
			  -I$(coreclr_srcdir)/src/gc \
			  -I$(coreclr_srcdir)/src/gc/env \
			  -I$(coreclr_srcdir)/src/gc/unix \
			  $(TMPFLAGS) \
			  $(LIBGC_CPPFLAGS) $(GLIB_CFLAGS) $(SHARED_CFLAGS)


coregc_sources = \
	$(coreclr_srcdir)/src/gc/gceventstatus.cpp \
	$(coreclr_srcdir)/src/gc/gceventstatus.h \
	$(coreclr_srcdir)/src/gc/gcconfig.cpp \
	$(coreclr_srcdir)/src/gc/gcconfig.h \
	$(coreclr_srcdir)/src/gc/gccommon.cpp \
	$(coreclr_srcdir)/src/gc/gcsvr.cpp \
	$(coreclr_srcdir)/src/gc/gcwks.cpp \
	$(coreclr_srcdir)/src/gc/handletable.cpp \
	$(coreclr_srcdir)/src/gc/handletable.h \
	$(coreclr_srcdir)/src/gc/handletablepriv.h \
	$(coreclr_srcdir)/src/gc/handletablecore.cpp \
	$(coreclr_srcdir)/src/gc/handletablescan.cpp \
	$(coreclr_srcdir)/src/gc/objecthandle.cpp \
	$(coreclr_srcdir)/src/gc/objecthandle.h \
	$(coreclr_srcdir)/src/gc/softwarewritewatch.cpp \
	$(coreclr_srcdir)/src/gc/softwarewritewatch.h \
	$(coreclr_srcdir)/src/gc/gchandletable.cpp \
	$(coreclr_srcdir)/src/gc/gchandletableimpl.h \
	$(coreclr_srcdir)/src/gc/gceesvr.cpp \
	$(coreclr_srcdir)/src/gc/gceewks.cpp \
	$(coreclr_srcdir)/src/gc/gcload.cpp \
	$(coreclr_srcdir)/src/gc/handletablecache.cpp \
	$(coreclr_srcdir)/src/gc/unix/gcenv.unix.cpp \
	$(coreclr_srcdir)/src/gc/unix/events.cpp \
	$(coreclr_srcdir)/src/gc/unix/cgroup.cpp \
	$(coreclr_srcdir)/src/gc/unix/cgroup.h \
	$(coreclr_srcdir)/src/gc/gcscan.cpp \
	$(coreclr_srcdir)/src/gc/gcscan.h


monocoregc_sources = \
	coregc-mono.cpp \
	coregc-mono.h \
	$(coregc_sources)

libmonocoregc_la_SOURCES = $(monocoregc_sources)
libmonocoregc_la_CFLAGS = $(COREGC_DEFINES)
libmonocoregc_la_CXXFLAGS = $(COREGC_DEFINES)


CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@) @CXX_ADD_CFLAGS@

if ENABLE_MSVC_ONLY

all-local:

	make -C $(top_srcdir)/msvc libgcmonocoregc

clean-local:

	make -C $(top_srcdir)/msvc clean-libgcmonocoregc

endif # ENABLE_MSVC_ONLY
