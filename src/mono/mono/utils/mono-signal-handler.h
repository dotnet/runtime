/*
 * mono-signal-handler.h: Handle signal handler differences across platforms
 *
 * Copyright (C) 2013 Xamarin Inc
 */

#ifndef __MONO_SIGNAL_HANDLER_H__
#define __MONO_SIGNAL_HANDLER_H__

#include "config.h"

#ifdef ENABLE_EXTENSION_MODULE
#include "../../../mono-extensions/mono/utils/mono-signal-handler.h"
#endif

/* Don't use this */
#ifndef MONO_SIGNAL_HANDLER_FUNC
#define MONO_SIGNAL_HANDLER_FUNC(access, name, arglist) access void name arglist
#endif

/*
 * Macros to work around signal handler differences on various platforms.
 *
 * To declare a signal handler function:
 * void MONO_SIG_HANDLER_SIGNATURE (handler_func)
 * To define a signal handler function:
 * MONO_SIG_HANDLER_FUNC(access, name)
 * To call another signal handler function:
 * handler_func (MONO_SIG_HANDLER_PARAMS);
 * To obtain the signal number:
 * int signo = MONO_SIG_HANDLER_GET_SIGNO ();
 * To obtain the signal context:
 * MONO_SIG_HANDLER_GET_CONTEXT ().
 * This will define a variable name 'ctx'.
 */

#ifdef HOST_WIN32
#define MONO_SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, EXCEPTION_POINTERS *_info, void *context)
#define MONO_SIG_HANDLER_FUNC(access, ftn) MONO_SIGNAL_HANDLER_FUNC (access, ftn, (int _dummy, EXCEPTION_POINTERS *_info, void *context))
#define MONO_SIG_HANDLER_PARAMS _dummy, _info, context
#define MONO_SIG_HANDLER_GET_SIGNO() (_dummy)
#define MONO_SIG_HANDLER_GET_INFO() (_info)
#define MONO_SIG_HANDLER_INFO_TYPE EXCEPTION_POINTERS
/* seh_vectored_exception_handler () passes in a CONTEXT* */
#define MONO_SIG_HANDLER_GET_CONTEXT \
    void *ctx = context;
#else
/* sigaction */
#define MONO_SIG_HANDLER_SIGNATURE(ftn) ftn (int _dummy, siginfo_t *_info, void *context)
#define MONO_SIG_HANDLER_FUNC(access, ftn) MONO_SIGNAL_HANDLER_FUNC (access, ftn, (int _dummy, siginfo_t *_info, void *context))
#define MONO_SIG_HANDLER_PARAMS _dummy, _info, context
#define MONO_SIG_HANDLER_GET_SIGNO() (_dummy)
#define MONO_SIG_HANDLER_GET_INFO() (_info)
#define MONO_SIG_HANDLER_INFO_TYPE siginfo_t
#define MONO_SIG_HANDLER_GET_CONTEXT \
    void *ctx = context;
#endif

#endif
