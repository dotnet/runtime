// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Signal used to inject activations into threads, shared by the CoreCLR PAL and NativeAOT.

#ifndef _ACTIVATION_SIGNAL_H_
#define _ACTIVATION_SIGNAL_H_

#include <signal.h>
#ifdef __APPLE__
#include <dlfcn.h>
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
    // TODO: Once our CI tools can get upgraded to xcode >= 15.4, replace the lookup below by this:
    // if (__builtin_available(macOS 14.4, iOS 17.4, tvOS 17.4, *))
    // {
    //    *status = dispatch_allow_send_signals(INJECT_ACTIVATION_SIGNAL);
    //    return true;
    // }
    void *libSystem = dlopen("/usr/lib/libSystem.dylib", RTLD_LAZY);
    if (libSystem == NULL)
    {
        return false;
    }

    int (*dispatch_allow_send_signals_ptr)(int) = (int (*)(int))dlsym(libSystem, "dispatch_allow_send_signals");
    if (dispatch_allow_send_signals_ptr == NULL)
    {
        return false;
    }

    *status = dispatch_allow_send_signals_ptr(INJECT_ACTIVATION_SIGNAL);
    return true;
}
#endif // __APPLE__

#endif // _ACTIVATION_SIGNAL_H_
