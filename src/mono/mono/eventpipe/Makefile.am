MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

if !ENABLE_MSVC_ONLY
if ENABLE_PERFTRACING

AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(GLIB_CFLAGS) $(SHARED_CFLAGS)

noinst_LTLIBRARIES = libeventpipe.la

eventpipe_sources = \
	ds-dump-protocol.c	\
	ds-dump-protocol.h	\
	ds-eventpipe-protocol.c	\
	ds-eventpipe-protocol.h	\
	ds-getter-setter.h	\
	ds-ipc.c	\
	ds-ipc.h	\
	ds-ipc-posix.c	\
	ds-ipc-posix.h	\
	ds-process-protocol.c	\
	ds-process-protocol.h	\
	ds-profiler-protocol.c	\
	ds-profiler-protocol.h	\
	ds-protocol.c	\
	ds-protocol.h	\
	ds-rt.h	\
	ds-rt-config.h	\
	ds-rt-mono.c	\
	ds-rt-mono.h	\
	ds-rt-types.h	\
	ds-rt-types-mono.h	\
	ds-server.c	\
	ds-server.h	\
	ds-types.h	\
	ep.c	\
	ep.h	\
	ep-block.c	\
	ep-block.h	\
	ep-buffer.c	\
	ep-buffer.h	\
	ep-buffer-manager.c	\
	ep-buffer-manager.h	\
	ep-config.c	\
	ep-config.h	\
	ep-config-internals.h	\
	ep-event.c	\
	ep-event.h	\
	ep-event-instance.h	\
	ep-event-instance.c	\
	ep-event-payload.c	\
	ep-event-payload.h	\
	ep-event-source.c	\
	ep-event-source.h	\
	ep-file.c	\
	ep-file.h	\
	ep-getter-setter.h	\
	ep-metadata-generator.c	\
	ep-metadata-generator.h	\
	ep-provider.c	\
	ep-provider.h	\
	ep-provider-internals.h	\
	ep-rt.h	\
	ep-rt-config.h	\
	ep-rt-config-mono.h	\
	ep-rt-mono.c	\
	ep-rt-mono.h	\
	ep-rt-types.h	\
	ep-rt-types-mono.h	\
	ep-thread.c	\
	ep-thread.h	\
	ep-types.h	\
	ep-session.c	\
	ep-session.h	\
	ep-session-provider.c	\
	ep-stack-contents.c	\
	ep-stack-contents.h	\
	ep-stream.c	\
	ep-stream.h


libeventpipe_la_SOURCES = $(eventpipe_sources)

endif # ENABLE_PERFTRACING
endif # !ENABLE_MSVC_ONLY

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@) @CXX_ADD_CFLAGS@
