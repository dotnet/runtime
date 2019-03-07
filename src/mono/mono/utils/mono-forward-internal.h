/**
 * \file
 *
 * (C) 2018 Microsoft, Inc.
 *
 */
#ifndef _MONO_UTILS_FORWARD_INTERNAL_
#define _MONO_UTILS_FORWARD_INTERNAL_

#include "mono-forward.h"

typedef struct MonoAotModule MonoAotModule;
typedef struct MonoHandleStack MonoHandleStack;
typedef struct MonoJitTlsData MonoJitTlsData;
typedef struct MonoLMF MonoLMF;
typedef struct MonoTrampInfo MonoTrampInfo;
#ifdef ENABLE_NETCORE
typedef struct _MonoThread MonoInternalThread;
#else
typedef struct _MonoInternalThread MonoInternalThread;
#endif
typedef struct _SgenThreadInfo SgenThreadInfo;

#endif
