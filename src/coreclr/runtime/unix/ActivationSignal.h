// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Signal used to inject activations into threads.

#ifndef _ACTIVATION_SIGNAL_H_
#define _ACTIVATION_SIGNAL_H_

#include <signal.h>
#ifdef __APPLE__
#include <dispatch/dispatch.h>
#endif

#ifdef SIGRTMIN
#define INJECT_ACTIVATION_SIGNAL SIGRTMIN
#else
#define INJECT_ACTIVATION_SIGNAL SIGUSR1
#endif

#ifdef __APPLE__
// Allows sending the activation signal to dispatch queue threads. Returns false when
// dispatch_allow_send_signals is not available; otherwise stores its result in *status.
inline bool AllowActivationSignalOnDispatchQueueThreads(int* status)
{
    if (__builtin_available(macOS 14.4, iOS 17.4, tvOS 17.4, *))
    {
        *status = dispatch_allow_send_signals(INJECT_ACTIVATION_SIGNAL);
        return true;
    }

    return false;
}
#endif // __APPLE__

#endif // _ACTIVATION_SIGNAL_H_
