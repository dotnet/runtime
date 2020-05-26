MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

if !ENABLE_MSVC_ONLY
if ENABLE_PERFTRACING

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(GLIB_CFLAGS) $(SHARED_CFLAGS)

noinst_LTLIBRARIES = libeventpipe.la

eventpipe_sources = \
	ep.c		\
	ep.h		\
	ep-block.c	\
	ep-block.h	\
	ep-block-internals.c	\
	ep-buffer.h	\
	ep-buffer-manager.c	\
	ep-buffer-manager.h	\
	ep-buffer-manager-internals.c	\
	ep-config.c	\
	ep-config.h	\
	ep-config-internals.c	\
	ep-event.h	\
	ep-event-instance.h	\
	ep-event-instance.c	\
	ep-event-instance-internals.c	\
	ep-event-internals.c	\
	ep-event-payload-internals.c	\
	ep-event-payload.h	\
	ep-event-source.c	\
	ep-event-source.h	\
	ep-event-source-internals.c	\
	ep-file.c	\
	ep-file.h	\
	ep-file-internals.c	\
	ep-internals.c	\
	ep-metadata-generator.c	\
	ep-metadata-generator.h	\
	ep-metadata-generator-internals.c	\
	ep-provider.c	\
	ep-provider.h	\
	ep-provider-internals.c	\
	ep-rt.h	\
	ep-rt-config.h	\
	ep-rt-config-mono.h	\
	ep-rt-mono.c	\
	ep-rt-mono.h	\
	ep-rt-types.h	\
	ep-rt-types-mono.h	\
	ep-thread.c	\
	ep-thread.h	\
	ep-thread-internals.c	\
	ep-types.h	\
	ep-session.c	\
	ep-session.h	\
	ep-session-internals.c	\
	ep-session-provider.c	\
	ep-session-provider-internals.h	\
	ep-session-provider-internals.c	\
	ep-stream.c	\
	ep-stream.h	\
	ep-stream-internals.c	\
	ep-stack-contents.h


libeventpipe_la_SOURCES = $(eventpipe_sources)

endif # ENABLE_PERFTRACING
endif # !ENABLE_MSVC_ONLY

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@) @CXX_ADD_CFLAGS@
