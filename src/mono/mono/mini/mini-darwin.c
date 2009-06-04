/*
 * mini-darwin.c: Darwin/MacOS support for Mono.
 *
 * Authors:
 *   Mono Team (mono-list@lists.ximian.com)
 *
 * Copyright 2001-2003 Ximian, Inc.
 * Copyright 2003-2008 Ximian, Inc.
 *
 * See LICENSE for licensing information.
 */
#include <config.h>
#include <signal.h>
#ifdef HAVE_ALLOCA_H
#include <alloca.h>
#endif
#ifdef HAVE_UNISTD_H
#include <unistd.h>
#endif
#include <math.h>
#ifdef HAVE_SYS_TIME_H
#include <sys/time.h>
#endif

#ifdef HAVE_VALGRIND_MEMCHECK_H
#include <valgrind/memcheck.h>
#endif

#include <mono/metadata/assembly.h>
#include <mono/metadata/loader.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/class.h>
#include <mono/metadata/object.h>
#include <mono/metadata/tokentype.h>
#include <mono/metadata/tabledefs.h>
#include <mono/metadata/threads.h>
#include <mono/metadata/appdomain.h>
#include <mono/metadata/debug-helpers.h>
#include <mono/io-layer/io-layer.h>
#include "mono/metadata/profiler.h"
#include <mono/metadata/profiler-private.h>
#include <mono/metadata/mono-config.h>
#include <mono/metadata/environment.h>
#include <mono/metadata/mono-debug.h>
#include <mono/metadata/gc-internal.h>
#include <mono/metadata/threads-types.h>
#include <mono/metadata/verify.h>
#include <mono/metadata/verify-internals.h>
#include <mono/metadata/mempool-internals.h>
#include <mono/metadata/attach.h>
#include <mono/utils/mono-math.h>
#include <mono/utils/mono-compiler.h>
#include <mono/utils/mono-counters.h>
#include <mono/utils/mono-logger.h>
#include <mono/utils/mono-mmap.h>
#include <mono/utils/dtrace.h>

#include "mini.h"
#include <string.h>
#include <ctype.h>
#include "trace.h"
#include "version.h"

#include "jit-icalls.h"

/* MacOS includes */
#include <mach/mach.h>
#include <mach/mach_error.h>
#include <mach/exception.h>
#include <mach/task.h>
#include <pthread.h>

#ifdef HAVE_SGEN_GC
#undef pthread_create
#undef pthread_join
#undef pthread_detach
#endif

/*
 * This code disables the CrashReporter of MacOS X by installing
 * a dummy Mach exception handler.
 */

/*
 * http://darwinsource.opendarwin.org/10.4.3/xnu-792.6.22/osfmk/man/exc_server.html
 */
extern boolean_t exc_server (mach_msg_header_t *request_msg, mach_msg_header_t *reply_msg);

/*
 * The exception message
 */
typedef struct {
	mach_msg_base_t msg;  /* common mach message header */
	char payload [1024];  /* opaque */
} mach_exception_msg_t;

/* The exception port */
static mach_port_t mach_exception_port = VM_MAP_NULL;

/*
 * Implicitly called by exc_server. Must be public.
 *
 * http://darwinsource.opendarwin.org/10.4.3/xnu-792.6.22/osfmk/man/catch_exception_raise.html
 */
kern_return_t
catch_exception_raise (
	mach_port_t exception_port,
	mach_port_t thread,
	mach_port_t task,
	exception_type_t exception,
	exception_data_t code,
	mach_msg_type_number_t code_count)
{
	/* consume the exception */
	return KERN_FAILURE;
}

/*
 * Exception thread handler.
 */
static
void *
mach_exception_thread (void *arg)
{
	for (;;) {
		mach_exception_msg_t request;
		mach_exception_msg_t reply;
		mach_msg_return_t result;

		/* receive from "mach_exception_port" */
		result = mach_msg (&request.msg.header,
				   MACH_RCV_MSG | MACH_RCV_LARGE,
				   0,
				   sizeof (request),
				   mach_exception_port,
				   MACH_MSG_TIMEOUT_NONE,
				   MACH_PORT_NULL);

		g_assert (result == MACH_MSG_SUCCESS);

		/* dispatch to catch_exception_raise () */
		exc_server (&request.msg.header, &reply.msg.header);

		/* send back to sender */
		result = mach_msg (&reply.msg.header,
				   MACH_SEND_MSG,
				   reply.msg.header.msgh_size,
				   0,
				   MACH_PORT_NULL,
				   MACH_MSG_TIMEOUT_NONE,
				   MACH_PORT_NULL);

		g_assert (result == MACH_MSG_SUCCESS);
	}
	return NULL;
}

static void
macosx_register_exception_handler ()
{
	mach_port_t task;
	pthread_attr_t attr;
	pthread_t thread;

	if (mach_exception_port != VM_MAP_NULL)
		return;

	task = mach_task_self ();

	/* create the "mach_exception_port" with send & receive rights */
	g_assert (mach_port_allocate (task, MACH_PORT_RIGHT_RECEIVE,
				      &mach_exception_port) == KERN_SUCCESS);
	g_assert (mach_port_insert_right (task, mach_exception_port, mach_exception_port,
					  MACH_MSG_TYPE_MAKE_SEND) == KERN_SUCCESS);

	/* create the exception handler thread */
	g_assert (!pthread_attr_init (&attr));
	g_assert (!pthread_attr_setdetachstate (&attr, PTHREAD_CREATE_DETACHED));
	g_assert (!pthread_create (&thread, &attr, mach_exception_thread, NULL));
	pthread_attr_destroy (&attr);

	/*
	 * register "mach_exception_port" as a receiver for the
	 * EXC_BAD_ACCESS exception
	 *
	 * http://darwinsource.opendarwin.org/10.4.3/xnu-792.6.22/osfmk/man/task_set_exception_ports.html
	 */
	g_assert (task_set_exception_ports (task, EXC_MASK_BAD_ACCESS,
					    mach_exception_port,
					    EXCEPTION_DEFAULT,
					    MACHINE_THREAD_STATE) == KERN_SUCCESS);
}

void
mono_runtime_install_handlers (void)
{
	macosx_register_exception_handler ();
	mono_runtime_posix_install_handlers ();
}
