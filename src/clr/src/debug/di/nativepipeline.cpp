// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//*****************************************************************************
// File: NativePipeline.cpp
// 

//
//*****************************************************************************

#include "stdafx.h"
#include "nativepipeline.h"

#if defined(ENABLE_EVENT_REDIRECTION_PIPELINE)
#include "eventredirection.h"
#include "eventredirectionpipeline.h"
#endif

#include "sstring.h"

//-----------------------------------------------------------------------------
// Returns null if redirection is not enabled, else returns a new redirection pipeline.
INativeEventPipeline * CreateEventRedirectionPipelineIfEnabled()
{
#if !defined(ENABLE_EVENT_REDIRECTION_PIPELINE)
    return NULL;
#else

    BOOL fEnabled = CLRConfig::GetConfigValue(CLRConfig::UNSUPPORTED_DbgRedirect) != 0;
    if (!fEnabled)
    {
        return NULL;
    }

    return new (nothrow) EventRedirectionPipeline();
#endif
}


//-----------------------------------------------------------------------------
// Allocate and return a pipeline object for this platform
// Has debug checks (such as for event redirection)
//
// Returns:
//    newly allocated pipeline object. Caller must call delete on it.
INativeEventPipeline * NewPipelineWithDebugChecks()
{
    CONTRACTL
    {
        NOTHROW;
    }
    CONTRACTL_END;
    INativeEventPipeline * pRedirection = CreateEventRedirectionPipelineIfEnabled();
    if (pRedirection != NULL)
    {
        return pRedirection;
    }

    return NewPipelineForThisPlatform();
}




