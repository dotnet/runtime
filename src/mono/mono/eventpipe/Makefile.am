MAKEFLAGS := $(MAKEFLAGS) --no-builtin-rules

if !ENABLE_MSVC_ONLY
if ENABLE_PERFTRACING

shared_eventpipe_path=$(top_srcdir)/../native/eventpipe
AM_CPPFLAGS = -I$(top_srcdir) -I$(top_srcdir)/mono $(GLIB_CFLAGS) $(SHARED_CFLAGS)

noinst_LTLIBRARIES = libeventpipe.la

shared_eventpipe_sources = \
	$(shared_eventpipe_path)/ds.c	\
	$(shared_eventpipe_path)/ds-dump-protocol.c	\
	$(shared_eventpipe_path)/ds-dump-protocol.h	\
	$(shared_eventpipe_path)/ds-eventpipe-protocol.c	\
	$(shared_eventpipe_path)/ds-eventpipe-protocol.h	\
	$(shared_eventpipe_path)/ds-getter-setter.h	\
	$(shared_eventpipe_path)/ds-ipc.c	\
	$(shared_eventpipe_path)/ds-ipc.h	\
	$(shared_eventpipe_path)/ds-process-protocol.c	\
	$(shared_eventpipe_path)/ds-process-protocol.h	\
	$(shared_eventpipe_path)/ds-profiler-protocol.c	\
	$(shared_eventpipe_path)/ds-profiler-protocol.h	\
	$(shared_eventpipe_path)/ds-protocol.c	\
	$(shared_eventpipe_path)/ds-protocol.h	\
	$(shared_eventpipe_path)/ds-rt.h	\
	$(shared_eventpipe_path)/ds-rt-config.h	\
	$(shared_eventpipe_path)/ds-rt-types.h	\
	$(shared_eventpipe_path)/ds-server.c	\
	$(shared_eventpipe_path)/ds-server.h	\
	$(shared_eventpipe_path)/ds-types.h	\
	$(shared_eventpipe_path)/ep.c	\
	$(shared_eventpipe_path)/ep.h	\
	$(shared_eventpipe_path)/ep-block.c	\
	$(shared_eventpipe_path)/ep-block.h	\
	$(shared_eventpipe_path)/ep-buffer.c	\
	$(shared_eventpipe_path)/ep-buffer.h	\
	$(shared_eventpipe_path)/ep-buffer-manager.c	\
	$(shared_eventpipe_path)/ep-buffer-manager.h	\
	$(shared_eventpipe_path)/ep-config.c	\
	$(shared_eventpipe_path)/ep-config.h	\
	$(shared_eventpipe_path)/ep-config-internals.h	\
	$(shared_eventpipe_path)/ep-event.c	\
	$(shared_eventpipe_path)/ep-event.h	\
	$(shared_eventpipe_path)/ep-event-instance.h	\
	$(shared_eventpipe_path)/ep-event-instance.c	\
	$(shared_eventpipe_path)/ep-event-payload.c	\
	$(shared_eventpipe_path)/ep-event-payload.h	\
	$(shared_eventpipe_path)/ep-event-source.c	\
	$(shared_eventpipe_path)/ep-event-source.h	\
	$(shared_eventpipe_path)/ep-file.c	\
	$(shared_eventpipe_path)/ep-file.h	\
	$(shared_eventpipe_path)/ep-getter-setter.h	\
	$(shared_eventpipe_path)/ep-json-file.c	\
	$(shared_eventpipe_path)/ep-json-file.h	\
	$(shared_eventpipe_path)/ep-metadata-generator.c	\
	$(shared_eventpipe_path)/ep-metadata-generator.h	\
	$(shared_eventpipe_path)/ep-provider.c	\
	$(shared_eventpipe_path)/ep-provider.h	\
	$(shared_eventpipe_path)/ep-provider-internals.h	\
	$(shared_eventpipe_path)/ep-rt.h	\
	$(shared_eventpipe_path)/ep-rt-config.h	\
	$(shared_eventpipe_path)/ep-rt-types.h	\
	$(shared_eventpipe_path)/ep-thread.c	\
	$(shared_eventpipe_path)/ep-thread.h	\
	$(shared_eventpipe_path)/ep-types.h	\
	$(shared_eventpipe_path)/ep-sample-profiler.c	\
	$(shared_eventpipe_path)/ep-sample-profiler.h	\
	$(shared_eventpipe_path)/ep-session.c	\
	$(shared_eventpipe_path)/ep-session.h	\
	$(shared_eventpipe_path)/ep-session-provider.c	\
	$(shared_eventpipe_path)/ep-stack-contents.c	\
	$(shared_eventpipe_path)/ep-stack-contents.h	\
	$(shared_eventpipe_path)/ep-stream.c	\
	$(shared_eventpipe_path)/ep-stream.h

if HOST_WIN32
shared_eventpipe_platform_sources = \
	$(shared_eventpipe_path)/ds-ipc-win32.c	\
	$(shared_eventpipe_path)/ds-ipc-win32.h
else
shared_eventpipe_platform_sources = \
	$(shared_eventpipe_path)/ds-ipc-posix.c	\
	$(shared_eventpipe_path)/ds-ipc-posix.h
endif

eventpipe_mono_shim_sources = \
	ds-rt-mono.c	\
	ds-rt-mono.h	\
	ds-rt-types-mono.h	\
	ep-rt-config-mono.h	\
	ep-rt-mono.c	\
	ep-rt-mono.h	\
	ep-rt-types-mono.h

libeventpipe_la_SOURCES = $(shared_eventpipe_sources) $(shared_eventpipe_platform_sources) $(eventpipe_mono_shim_sources)

endif # ENABLE_PERFTRACING
endif # !ENABLE_MSVC_ONLY

CFLAGS := $(filter-out @CXX_REMOVE_CFLAGS@, @CFLAGS@) @CXX_ADD_CFLAGS@
