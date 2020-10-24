// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma once

struct CallsiteDetails
{
    // The signature of the current call
    MetaSig MetaSig;

    // The current call frame
    FramedMethodFrame *Frame;

    // The relevant method for the callsite
    MethodDesc *MethodDesc;

    // Is the callsite for a delegate
    // Note the relevant method may _not_ be a delegate
    BOOL IsDelegate;

    // Flags for callsite
    enum
    {
        None            = 0x0,
        BeginInvoke     = 0x01,
        EndInvoke       = 0x02,
        Ctor            = 0x04,
    };
    INT32 Flags;
};

namespace CallsiteInspect
{
    // Get all arguments and associated argument details at the supplied callsite
    void GetCallsiteArgs(
        _In_ CallsiteDetails &callsite,
        _Outptr_ PTRARRAYREF *args,
        _Outptr_ BOOLARRAYREF *argsIsByRef,
        _Outptr_ PTRARRAYREF *argsTypes);

    // Properly propagate out parameters
    void PropagateOutParametersBackToCallsite(
        _In_ PTRARRAYREF outParams,
        _In_ OBJECTREF retVal,
        _In_ CallsiteDetails &callsite);
}
