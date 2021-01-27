// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Runtime headers
#include "common.h"

#include "interoplibinterface.h"

void Interop::OnGCStarted(_In_ int nCondemnedGeneration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //
    // Note that we could get nested GCStart/GCEnd calls, such as :
    // GCStart for Gen 2 background GC
    //    GCStart for Gen 0/1 foregorund GC
    //    GCEnd   for Gen 0/1 foreground GC
    //    ....
    // GCEnd for Gen 2 background GC
    //
    // The nCondemnedGeneration >= 2 check takes care of this nesting problem
    //
    // See Interop::OnGCFinished()
    if (nCondemnedGeneration >= 2)
    {
#ifdef FEATURE_COMWRAPPERS
        ComWrappersNative::OnBackgroundGCStarted();
#endif // FEATURE_COMWRAPPERS
    }
}

void Interop::OnGCFinished(_In_ int nCondemnedGeneration)
{
    CONTRACTL
    {
        NOTHROW;
        GC_NOTRIGGER;
    }
    CONTRACTL_END;

    //
    // Note that we could get nested GCStart/GCEnd calls, such as :
    // GCStart for Gen 2 background GC
    //    GCStart for Gen 0/1 foregorund GC
    //    GCEnd   for Gen 0/1 foreground GC
    //    ....
    // GCEnd for Gen 2 background GC
    //
    // The nCondemnedGeneration >= 2 check takes care of this nesting problem
    //
    // See Interop::OnGCStarted()
    if (nCondemnedGeneration >= 2)
    {
#ifdef FEATURE_COMWRAPPERS
        ComWrappersNative::OnBackgroundGCFinished();
#endif // FEATURE_COMWRAPPERS
    }
}